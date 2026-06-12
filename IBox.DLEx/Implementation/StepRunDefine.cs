using DocumentFormat.OpenXml;
using Esprima;
using Esprima.Ast;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Model;
using IBox.MailService;
using IBox.Workflow.Model;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Dynamic;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Serilog;
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace IBox.DLEx.Implementation
{
    public partial class ExecuteWF
    {
        private static readonly Regex RxInfiniteWhile = new Regex(@"while\s*\(\s*true\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RxInfiniteFor = new Regex(@"for\s*\(\s*;\s*;\s*\)", RegexOptions.Compiled);
        private static readonly Regex RxUnicodeEscape = new(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private dynamic runStart(WFStep step, object param, string tenantId)
        {
            Log.Information("[Step {StepId}] Starting execution", step.Id);
            try
            {
                if (step.ChildSteps == null)
                {
                    return param;
                }

                string payloadJson;

                if (param is IDictionary<string, object> dict)
                {
                    var fieldsOnly = new Dictionary<string, object>();
                    foreach (var kv in dict)
                    {
                        if (string.Equals(kv.Key, "__files", StringComparison.OrdinalIgnoreCase))
                            continue;
                        fieldsOnly[kv.Key] = kv.Value;
                    }
                    payloadJson = JsonConvert.SerializeObject(fieldsOnly);
                }
                else
                {
                    payloadJson = param.ToString();
                }

                var res = this._modelControl.BindingData(step.Response, payloadJson, tenantId);

                if (step.SaveResponseToCache ?? false)
                {
                    // Lưu header như cũ
                    this._headerDictionary.ToList().ForEach(ptr =>
                    {
                        saveCahe($"{ptr.Key.ToLower()}", new WFCache()
                        {
                            ObjType = typeof(string),
                            Obj = ptr.Value.FirstOrDefault()
                        });
                    });

                    // Lưu body tuỳ theo loại
                    if (param is IDictionary<string, object> dictParam && dictParam.ContainsKey("__files")) { }
                    else
                    {
                        // Không có file
                        saveCahe(step.Id ?? throw new IboxLog($"Step id is null", tenantId, "Error"), new WFCache()
                        {
                            ObjType = this._modelControl.GetType(step.Response, tenantId),
                            Obj = res
                        });
                    }
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    WorkflowId = step.WfId,
                    RequestBody = ParseJsonOrKeepString(payloadJson),
                    ResponseBody = res,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty
                });

                return res;
            }
            catch (Exception ex)
            {
                saveDebug(new ModelXWorkflowDebug
                {
                    WorkflowId = step.WfId,
                    RequestBody = param?.ToString(),
                    ResponseBody = ex.ToString(),
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = ex.Message
                });
                throw;
            }
        }

        private static HttpClientHandler CreateHttpClientHandler(bool ignoreSsl)
        {
            var handler = new HttpClientHandler();

            if (ignoreSsl)
            {
                // Chỉ dùng cho môi trường dev/test
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                // or:
                // handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
            }

            return handler;
        }

        private (dynamic, List<WFStep>) runForwardFileStep(
            WFStep step,
            object param,
            string wfName,
            string tenantId,
            IFormFileCollection? uploadedFiles = null)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog($"Can not found Config in step {step.Id}", tenantId);
                }

                var cfg = JsonConvert.DeserializeObject<ForwardFileWFConfig>(step.Config) ?? new ForwardFileWFConfig();

                if (cfg.Source == null || string.IsNullOrEmpty(cfg.Source.Url))
                {
                    throw new IboxLog("ForwardFileWFConfig.Source.Url is required", tenantId);
                }
                if (cfg.Target == null || string.IsNullOrEmpty(cfg.Target.Url))
                {
                    throw new IboxLog("ForwardFileWFConfig.Target.Url is required", tenantId);
                }

                var mode = (cfg.Mode ?? "form-data").ToLowerInvariant();

                var sourceUrl = BuildUrlWithParams(cfg.Source.Url, cfg.Source.Params, param, tenantId);
                var targetUrl = BuildUrlWithParams(cfg.Target.Url, cfg.Target.Params, param, tenantId);

                // Replace property cache cho Target Headers
                if (cfg.Target.Headers != null && cfg.Target.Headers.Any())
                {
                    var keys = cfg.Target.Headers.Keys.ToList();
                    foreach (var k in keys)
                    {
                        var val = cfg.Target.Headers[k] ?? "";
                        if (val.Contains('['))
                        {
                            val = replaceProperty(val, "", param);
                            var tags = findTag(val);
                            tags.ForEach(ptr =>
                            {
                                if (string.IsNullOrEmpty(ptr)) return;
                                var objid = ptr.Split('.')[0];
                                var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                val = replacePropertyCache(objid, val, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                            });
                            cfg.Target.Headers[k] = val;
                        }
                    }
                }

                // dùng chung key log cho cả A & B
                string keyExecuteRunApiThirdPartyA = Guid.NewGuid().ToString();
                string keyExecuteRunApiThirdPartyB = Guid.NewGuid().ToString();
                DateTime createDate = DateTime.Now;

                using (var handlerSource = CreateHttpClientHandler(false))
                using (var handlerTarget = CreateHttpClientHandler(false))
                using (var clientSource = new HttpClient(handlerSource))
                using (var clientTarget = new HttpClient(handlerTarget))
                {
                    if (cfg.Timeout > 0)
                    {
                        clientSource.Timeout = TimeSpan.FromSeconds(cfg.Timeout);
                        clientTarget.Timeout = TimeSpan.FromSeconds(cfg.Timeout);
                    }

                    // 1. CALL A:  download file
                    var respSource = clientSource.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

                    if (!respSource.IsSuccessStatusCode)
                    {
                        var errBody = "";
                        try { errBody = respSource.Content.ReadAsStringAsync().GetAwaiter().GetResult(); } catch { }

                        // DEBUG cho SOURCE lỗi
                        saveDebug(new ModelXWorkflowDebug
                        {
                            RequestBody = new { SourceUrl = sourceUrl, SourceParams = cfg.Source.Params },
                            ResponseBody = errBody,
                            StepID = step.Id,
                            StepName = step.Description,
                            ErrorMessage = $"Download from source failed: {(int)respSource.StatusCode} - {respSource.ReasonPhrase}",
                            WorkflowId = step.WfId
                        });

                        // LOG FILE cho SOURCE lỗi
                        _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                        {
                            TenantId = tenantId,
                            Wfid = step.WfId,
                            WFName = wfName,
                            StepName = step.Description,
                            StepId = step.Id,
                            SiteRun = IBGlobalConfig.ThisSite,
                            KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdPartyA,
                            Status = ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                            StatusCode = ((int)respSource.StatusCode).ToString(),
                            Url = sourceUrl,
                            Request = "",
                            Response = errBody,
                            Headers = "{}",
                            Reason = respSource.ReasonPhrase,
                            CreatedDate = createDate,
                            ModificationDate = DateTime.Now
                        });

                        // XỬ LÝ EXCEPTION STEPS cho SOURCE
                        var hasExceptionSteps = step.ChildSteps?.Any(ptr => ptr.IsExceptionStep == true) ?? false;

                        if (!hasExceptionSteps)
                        {
                            // ⚠️ KHÔNG CÓ EXCEPTION STEPS → Chạy childSteps thông thường
                            var normalChildSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                            return (errBody, normalChildSteps);
                        }

                        // ✅ CÓ EXCEPTION STEPS → Chạy tất cả exception steps (default)
                        var exSteps = step.ChildSteps?.Where(s => s.IsExceptionStep == true).ToList();
                        if (exSteps != null && exSteps.Any())
                            return (errBody, exSteps);

                        throw new IboxLog(
                            $"ForwardFileStep:  download from source failed with status {(int)respSource.StatusCode}",
                            tenantId
                        );
                    }

                    // đọc thành byte[] tránh vấn đề stream chunked
                    var fileBytes = respSource.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    var sourceContentType = respSource.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    var sourceContentLength = fileBytes?.Length ?? 0;

                    // DEBUG cho SOURCE thành công
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = new { SourceUrl = sourceUrl, SourceParams = cfg.Source.Params },
                        ResponseBody = new
                        {
                            StatusCode = (int)respSource.StatusCode,
                            ReasonPhrase = respSource.ReasonPhrase,
                            ContentType = sourceContentType,
                            ContentLength = sourceContentLength
                        },
                        StepID = step.Id,
                        StepName = step.Description,
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });

                    // LOG FILE cho SOURCE thành công
                    _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                    {
                        TenantId = tenantId,
                        Wfid = step.WfId,
                        WFName = wfName,
                        StepName = step.Description,
                        StepId = step.Id,
                        SiteRun = IBGlobalConfig.ThisSite,
                        KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdPartyA,
                        Status = ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                        StatusCode = ((int)respSource.StatusCode).ToString(),
                        Url = sourceUrl,
                        Request = "",
                        Response = $"[binary; length={sourceContentLength}]",
                        Headers = "{}",
                        Reason = null,
                        CreatedDate = createDate,
                        ModificationDate = DateTime.Now
                    });

                    // 2. xác định tên file
                    var fileName = !string.IsNullOrEmpty(cfg.TargetFileName) ? cfg.TargetFileName: GetFileNameFromResponseOrUrl(respSource, sourceUrl);

                    // lấy Content-Type từ config B (nếu có), ưu tiên hơn source
                    string? ctFromConfig = null;
                    if (cfg.Target.Headers != null && cfg.Target.Headers.TryGetValue("Content-Type", out var ctCfg) && !string.IsNullOrWhiteSpace(ctCfg))
                    {
                        ctFromConfig = ctCfg.Trim();
                    }

                    // 3. build content đẩy sang B
                    HttpContent uploadContent;
                    var fieldName = string.IsNullOrEmpty(cfg.TargetFieldName) ? "file" : cfg.TargetFieldName;
                    var formFieldsLogRuntime = new List<object>();

                    if (mode == "binary")
                    {
                        var fileContent = new ByteArrayContent(fileBytes ?? Array.Empty<byte>());

                        var finalCt = !string.IsNullOrEmpty(ctFromConfig) ? ctFromConfig : sourceContentType;
                        try
                        {
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(finalCt);
                        }
                        catch
                        {
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        }

                        uploadContent = fileContent;

                        formFieldsLogRuntime.Add(new
                        {
                            Type = "file",
                            FileName = fileName,
                            ContentType = finalCt,
                            Length = sourceContentLength
                        });
                    }
                    else
                    {
                        var multipart = new MultipartFormDataContent();

                        var fileContent = new ByteArrayContent(fileBytes ?? Array.Empty<byte>());
                        try
                        {
                            var finalCt = !string.IsNullOrEmpty(ctFromConfig) ? ctFromConfig : sourceContentType;
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(finalCt);
                        }
                        catch
                        {
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        }

                        multipart.Add(fileContent, fieldName, fileName);

                        formFieldsLogRuntime.Add(new
                        {
                            Key = fieldName,
                            Type = "file",
                            FileName = fileName,
                            ContentType = fileContent.Headers.ContentType?.MediaType,
                            FromSourceUrl = sourceUrl,
                            Length = sourceContentLength
                        });

                        if (cfg.Target.Body?.FormFields != null)
                        {
                            foreach (var ff in cfg.Target.Body.FormFields)
                            {
                                if (string.Equals(ff.Type, "file", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                var fname = ff.Key ?? ff.Id ?? "field";
                                var rawValue = ff.Value ?? "";

                                rawValue = replaceProperty(rawValue, "", param);

                                var tags = findTag(rawValue);
                                foreach (var t in tags)
                                {
                                    if (string.IsNullOrEmpty(t)) continue;
                                    var objid = t.Split('.')[0];
                                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                    rawValue = replacePropertyCache(
                                        objid,
                                        rawValue,
                                        "",
                                        cacheEntry.Value?.Obj ?? new { },
                                        tenantId
                                    );
                                }

                                multipart.Add(new StringContent(rawValue ?? ""), fname);

                                formFieldsLogRuntime.Add(new
                                {
                                    Key = fname,
                                    Type = ff.Type ?? "text",
                                    Value = rawValue
                                });
                            }
                        }

                        uploadContent = multipart;
                    }

                    // 4. CALL B
                    var targetMethod = string.IsNullOrEmpty(cfg.Target.Method)? "POST": cfg.Target.Method.ToUpperInvariant();

                    var targetRequest = new HttpRequestMessage(new HttpMethod(targetMethod), targetUrl);
                    targetRequest.Content = uploadContent;

                    AddHeadersToRequest(targetRequest, uploadContent, cfg.Target.Headers);

                    var respTarget = clientTarget.SendAsync(targetRequest).GetAwaiter().GetResult();

                    var respBodyB = "";
                    try { respBodyB = respTarget.Content.ReadAsStringAsync().GetAwaiter().GetResult(); } catch { }

                    var resParseBodyB = ParseJsonOrKeepString(respBodyB);

                    object? bodyLogForTarget = null;
                    if (cfg.Target.Body != null)
                    {
                        bodyLogForTarget = new
                        {
                            Type = cfg.Target.Body.Type,
                            RawFormat = cfg.Target.Body.RawFormat,
                            RawContent = cfg.Target.Body.RawContent,
                            BinaryValue = cfg.Target.Body.BinaryValue,
                            FormFields = formFieldsLogRuntime,
                            UrlEncodedFields = cfg.Target.Body.UrlEncodedFields?
                                .Select(f => new { Key = f.Key, Value = f.Value })
                                .ToList()
                        };
                    }

                    // DEBUG cho TARGET
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = new
                        {
                            Target = new
                            {
                                Url = targetUrl,
                                Method = targetMethod,
                                Params = cfg.Target.Params,
                                Headers = cfg.Target.Headers,
                                Body = bodyLogForTarget
                            }
                        },
                        ResponseBody = resParseBodyB,
                        StepID = step.Id,
                        StepName = step.Description,
                        ErrorMessage = respTarget.IsSuccessStatusCode ? string.Empty : respTarget.ReasonPhrase,
                        WorkflowId = step.WfId
                    });

                    // LOG FILE cho TARGET
                    _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                    {
                        TenantId = tenantId,
                        Wfid = step.WfId,
                        WFName = wfName,
                        StepName = step.Description,
                        StepId = step.Id,
                        SiteRun = IBGlobalConfig.ThisSite,
                        KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdPartyB,
                        Status = respTarget.IsSuccessStatusCode ? ExecuteRunApiThirdPartyStatus.Complete.ToString() : ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                        StatusCode = ((int)respTarget.StatusCode).ToString(),
                        Url = targetUrl,
                        Request = SerializeHttpContentForLog(uploadContent).GetAwaiter().GetResult(),
                        Response = respBodyB,
                        Headers = JsonConvert.SerializeObject(cfg.Target.Headers),
                        Reason = respTarget.IsSuccessStatusCode ? null : respTarget.ReasonPhrase,
                        CreatedDate = createDate,
                        ModificationDate = DateTime.Now
                    });

                    // ===================== XỬ LÝ KHI API THÀNH CÔNG =====================
                    if (respTarget.IsSuccessStatusCode)
                    {
                        // binding nếu cần
                        object bindingdata = respBodyB;

                        if (!string.IsNullOrEmpty(step.Response))
                        {
                            try
                            {
                                // Check kiểm tra xml hay Json để convert
                                if (!string.IsNullOrEmpty(respBodyB) && respBodyB.TrimStart().StartsWith("<"))
                                {
                                    bindingdata = this._modelControl.BindingDataXML(step.Response, respBodyB, tenantId);
                                }
                                else if (!string.IsNullOrEmpty(respBodyB) && respBodyB.TrimStart().StartsWith("["))
                                {
                                    JToken jToken = JToken.Parse(respBodyB);
                                    JToken wrapped = new JObject { ["data_convert_list_object"] = jToken };
                                    bindingdata = this._modelControl.BindingData(step.Response, JsonConvert.SerializeObject(wrapped), tenantId);
                                }
                                else
                                {
                                    bindingdata = this._modelControl.BindingData(step.Response, respBodyB, tenantId);
                                }
                            }
                            catch
                            {
                                bindingdata = respBodyB;
                            }
                        }

                        // Lưu cache nếu cần
                        if (step.SaveResponseToCache ?? false)
                        {
                            saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                            {
                                ObjType = this._modelControl.GetType(step.Response, tenantId),
                                Obj = bindingdata
                            });
                        }

                        saveDebug(new ModelXWorkflowDebug
                        {
                            RequestBody = param,
                            ResponseBody = bindingdata,
                            StepID = step.Id,
                            StepName = step.Description,
                            ErrorMessage = string.Empty,
                            WorkflowId = step.WfId
                        });

                        // ✅ THÀNH CÔNG → Chạy childSteps THÔNG THƯỜNG
                        var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                        return (bindingdata, childSteps);
                    }
                    // ===================== XỬ LÝ KHI API LỖI (IsSuccessStatusCode = false) =====================
                    else
                    {
                        var responseApi = respBodyB ?? string.Empty;

                        // Binding data error
                        object? bindingdataError = null;
                        if (!string.IsNullOrEmpty(responseApi) && responseApi.TrimStart().StartsWith("<"))
                            bindingdataError = this._modelControl.BindingDataXML(step.Response, responseApi, tenantId);
                        else
                            bindingdataError = this._modelControl.BindingData(step.Response, responseApi, tenantId);

                        // ✅ Kiểm tra xem có exception steps không
                        var hasExceptionSteps = step.ChildSteps?.Any(ptr => ptr.IsExceptionStep == true) ?? false;

                        if (!hasExceptionSteps)
                        {
                            // ⚠️ KHÔNG CÓ EXCEPTION STEPS → Chạy childSteps thông thường
                            if (step.SaveResponseToCache ?? false)
                            {
                                saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                {
                                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                                    Obj = bindingdataError
                                });
                            }

                            var normalChildSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                            return (bindingdataError, normalChildSteps);
                        }

                        // ✅ CÓ EXCEPTION STEPS → Chạy tất cả exception steps (default)
                        var allExceptionSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == true).ToList();

                        // Lưu cache nếu cần
                        if (step.SaveResponseToCache ?? false)
                        {
                            saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                            {
                                ObjType = this._modelControl.GetType(step.Response, tenantId),
                                Obj = bindingdataError
                            });
                        }

                        // ✅ Chạy tất cả exception steps
                        return (bindingdataError, allExceptionSteps);
                    }
                }
            }
            catch (Exception ex)
            {
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = "",
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = ex.Message,
                    WorkflowId = step.WfId
                });

                throw;
            }
        }

        private void AddHeadersToRequest(HttpRequestMessage request, HttpContent content, Dictionary<string, string>? headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return;
            }

            foreach (var kv in headers)
            {
                var label = kv.Key?.Trim();
                var value = kv.Value ?? string.Empty;

                if (string.IsNullOrEmpty(label))
                    continue;

                // 1. Authorization
                if (string.Equals(label, "Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.TryAddWithoutValidation(label, value);
                    continue;
                }

                // 2. Content-Type -> content headers
                if (string.Equals(label, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
                    }
                    catch { }
                    continue;
                }

                // 3. Content-Disposition -> content headers (attachment;filename=...)
                if (string.Equals(label, "Content-Disposition", StringComparison.OrdinalIgnoreCase))
                {
                    if (ContentDispositionHeaderValue.TryParse(value, out var cd))
                    {
                        content.Headers.ContentDisposition = cd;
                    }
                    else
                    {
                        content.Headers.TryAddWithoutValidation(label, value);
                    }
                    continue;
                }

                // 4. others
                if (!request.Headers.TryAddWithoutValidation(label, value))
                {
                    content.Headers.TryAddWithoutValidation(label, value);
                }
            }
        }

        private string BuildUrlWithParams(string baseUrl, List<ParamItem>? @params, object param, string tenantId)
        {
            var url = replaceProperty(baseUrl ?? "", "", param);
            var tags = findTag(url);
            foreach (var t in tags)
            {
                if (string.IsNullOrEmpty(t)) continue;
                var objid = t.Split('.')[0];
                var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                url = replacePropertyCache(objid, url, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
            }

            if (@params == null || !@params.Any())
                return url;

            var uriBuilder = new UriBuilder(url);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

            foreach (var p in @params)
            {
                if (string.IsNullOrEmpty(p.Key)) continue;
                var v = p.Value ?? "";
                v = replaceProperty(v, "", param);

                var vt = findTag(v);
                foreach (var t in vt)
                {
                    if (string.IsNullOrEmpty(t)) continue;
                    var objid = t.Split('.')[0];
                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                    v = replacePropertyCache(objid, v, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                }

                query[p.Key] = v;
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.ToString();
        }

        private string GetFileNameFromResponseOrUrl(HttpResponseMessage resp, string urlFallback)
        {
            // ưu tiên Content-Disposition
            var cd = resp.Content.Headers.ContentDisposition;
            if (cd != null && !string.IsNullOrEmpty(cd.FileName))
            {
                return cd.FileName.Trim('"');
            }

            // fallback: lấy từ URL
            try
            {
                var uri = new Uri(urlFallback);
                var name = Path.GetFileName(uri.AbsolutePath);
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { }

            return "file";
        }

        private (dynamic, List<WFStep>) runCallXAPI(WFStep step, object param, string wfName, string tenantId, IFormFileCollection? uploadedFiles = null)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog($"Can not found Config in step {step.Id}", tenantId);
                }

                var cfg = JsonConvert.DeserializeObject<XRestAPIRequestWFConfig>(step.Config) ?? new XRestAPIRequestWFConfig();

                cfg.TargetUrl = replaceTargetUrl(cfg.TargetUrl ?? string.Empty, param, tenantId);

                if (cfg.Headers != null && cfg.Headers.Any())
                {
                    var keys = cfg.Headers.Keys.ToList();
                    foreach (var k in keys)
                    {
                        var val = cfg.Headers[k] ?? "";
                        if (val.Contains('['))
                        {
                            val = replaceProperty(val, "", param);
                            var tags = findTag(val);
                            tags.ForEach(ptr =>
                            {
                                if (string.IsNullOrEmpty(ptr)) return;
                                var objid = ptr.Split('.')[0];
                                var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                val = replacePropertyCache(objid, val, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                            });
                            cfg.Headers[k] = val;
                        }
                    }
                }
                else
                {
                    cfg.Headers = new Dictionary<string, string>();
                }

                // Add Basic auth header if configured in Authen
                if (cfg.Authen != null && cfg.Authen.Type == AuthorType.Basic)
                {
                    string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                   .GetBytes(cfg.Authen.UserName + ":" + cfg.Authen.Password));
                    cfg.Headers["Authorization"] = $"Basic {encoded}";
                }

                // ✅ THÊM: Detect body type TRƯỚC KHI xử lý headers
                string bodyTypeNormalized = "raw";
                if (cfg.Body != null && !string.IsNullOrEmpty(cfg.Body.Type))
                {
                    var t = cfg.Body.Type.Trim().ToLowerInvariant();
                    if (t == "form-data" || t == "formdata" || t == "multipart/form-data")
                        bodyTypeNormalized = "form-data";
                    else if (t == "x-www-form-urlencoded" || t == "urlencoded" || t == "url-encoded")
                        bodyTypeNormalized = "x-www-form-urlencoded";
                    else if (t == "binary" || t == "file" || t == "octet-stream")
                        bodyTypeNormalized = "binary";
                    else if (t == "raw" || t == "text" || t == "json" || t == "xml")
                        bodyTypeNormalized = "raw";
                    else
                        bodyTypeNormalized = t;
                }

                bool isFormData = (bodyTypeNormalized == "form-data");

                // build HttpRequest
                using (var handler = CreateHttpClientHandler(false))
                using (var httpClient = new HttpClient(handler))
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                    if (cfg.Timeout > 0)
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(cfg.Timeout);
                    }

                    // --- Xử lý headers tự động phân loại (request header vs content header) ---
                    Dictionary<string, string> pendingContentHeaders = new(StringComparer.OrdinalIgnoreCase);

                    if (cfg.Headers != null)
                    {
                        foreach (var kv in cfg.Headers)
                        {
                            var label = kv.Key;
                            var value = kv.Value ?? "";

                            // QUAN TRỌNG: Bỏ qua Content-Type nếu body là form-data
                            if (isFormData && string.Equals(label, "Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                // BỎ QUA - Để MultipartFormDataContent tự động set
                                continue;
                            }
                            // Tạm thời lưu: lát nữa thử add vào content, nếu fail thì add vào request header
                            pendingContentHeaders[label] = value;
                        }
                    }

                    // build content
                    HttpContent content = BuildContentFromConfig(
                        cfg,
                        this._modelControl.BindingData(step.Param, param, tenantId),
                        uploadedFiles,
                        tenantId
                    );

                    object requestBodyInfo = null;
                    try
                    {
                        requestBodyInfo = ExtractBodyInfoForLog(content, cfg.Body?.Type ?? "raw");
                    }
                    catch (Exception ex)
                    {
                        requestBodyInfo = new { Error = $"Cannot build request info: {ex.Message}" };
                    }

                    foreach (var kv in pendingContentHeaders)
                    {
                        var label = kv.Key;
                        var value = kv.Value;

                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(label, value);
                    }

                    HttpResponseMessage resp = null;
                    string respBody = "";
                    string keyExecuteRunApiThirdParty = Guid.NewGuid().ToString();
                    DateTime createDate = DateTime.Now;

                    // If ExecuteAsync true -> run in background task and immediately return success-like response
                    if (cfg.ExecuteAsync)
                    {
                        // Deploy target workflows if specified in AfterExecute
                        _workflowControl.DeployIfNotExist(cfg.AfterExecute?.WfExecuteSuccess, tenantId);
                        _workflowControl.DeployIfNotExist(cfg.AfterExecute?.WfExecuteError, tenantId);

                        Task.Run(() =>
                        {
                            try
                            {
                                HttpResponseMessage asyncResp;
                                var method = string.IsNullOrEmpty(cfg.Method) ? "POST" : cfg.Method.ToUpperInvariant();
                                if (method == "PUT")
                                    asyncResp = httpClient.PutAsync(cfg.TargetUrl, content).GetAwaiter().GetResult();
                                else if (method == "GET")
                                    asyncResp = httpClient.GetAsync(cfg.TargetUrl).GetAwaiter().GetResult();
                                else
                                    asyncResp = httpClient.PostAsync(cfg.TargetUrl, content).GetAwaiter().GetResult();

                                var respBody = asyncResp.Content.ReadAsStringAsync().Result;

                                // log
                                _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                                {
                                    TenantId = tenantId,
                                    Wfid = step.WfId,
                                    WFName = wfName,
                                    StepName = step.Description,
                                    StepId = step.Id,
                                    SiteRun = IBGlobalConfig.ThisSite,
                                    KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                                    Status = (asyncResp.IsSuccessStatusCode ? ExecuteRunApiThirdPartyStatus.Complete.ToString() : ExecuteRunApiThirdPartyStatus.Fail.ToString()),
                                    StatusCode = ((int)asyncResp.StatusCode).ToString(),
                                    Url = cfg.TargetUrl,
                                    Request = SerializeHttpContentForLog(content).GetAwaiter().GetResult(),
                                    Response = respBody,
                                    Headers = JsonConvert.SerializeObject(cfg.Headers),
                                    Reason = null,
                                    CreatedDate = createDate,
                                    ModificationDate = DateTime.Now
                                });

                                if (asyncResp.IsSuccessStatusCode)
                                {
                                    if (!string.IsNullOrEmpty(cfg.AfterExecute?.WfExecuteSuccess))
                                    {
                                        ResponseDataSuccess(cfg.AfterExecute.WfExecuteSuccess, respBody, param, step.Param, step.Response, tenantId);
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(cfg.AfterExecute?.WfExecuteError))
                                    {
                                        ResponseDataError(cfg.AfterExecute.WfExecuteError, param, step.Param, step.Response, tenantId);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                                {
                                    TenantId = tenantId,
                                    Wfid = step.WfId,
                                    WFName = wfName,
                                    StepName = step.Description,
                                    StepId = step.Id,
                                    SiteRun = IBGlobalConfig.ThisSite,
                                    KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                                    Status = ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                                    StatusCode = "500",
                                    Url = cfg.TargetUrl,
                                    Request = SerializeHttpContentForLog(content).GetAwaiter().GetResult(),
                                    Response = null,
                                    Headers = JsonConvert.SerializeObject(cfg.Headers),
                                    Reason = ex.Message,
                                    CreatedDate = createDate,
                                    ModificationDate = DateTime.Now
                                });

                                if (!string.IsNullOrEmpty(cfg.AfterExecute?.WfExecuteError))
                                {
                                    ResponseDataError(cfg.AfterExecute.WfExecuteError, param, step.Param, step.Response, tenantId);
                                }
                            }
                        });

                        // Trả về giả lập OK để tiếp tục luồng chính (pattern giống callRestAPI)
                        var fakeResponse = new RestAPIResponse()
                        {
                            Result = (step.Param == step.Response) ? JsonConvert.SerializeObject(param) : _modelControl.BindingDataDump(step.Response ?? "", tenantId).ToString(),
                            HttpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK }
                        };

                        // map giống runCallAPI (save cache nếu cần)
                        if (step.SaveResponseToCache ?? false)
                        {
                            saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                            {
                                ObjType = this._modelControl.GetType(step.Response, tenantId),
                                Obj = fakeResponse.Result
                            });
                        }

                        saveDebug(new ModelXWorkflowDebug
                        {
                            RequestBody = param,
                            ResponseBody = fakeResponse.Result,
                            StepID = step.Id,
                            StepName = step.Description,
                            ErrorMessage = string.Empty,
                            WorkflowId = step.WfId
                        });

                        var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();
                        return (fakeResponse.Result, childSteps);
                    }
                    else
                    {
                        try
                        {
                            var method = string.IsNullOrEmpty(cfg.Method) ? "POST" : cfg.Method.ToUpperInvariant();
                            
                            if (method == "PUT")
                                resp = httpClient.PutAsync(cfg.TargetUrl, content).GetAwaiter().GetResult();
                            else if (method == "GET")
                            {
                                var request = new HttpRequestMessage(HttpMethod.Get, cfg.TargetUrl);

                                if (cfg.Headers != null)
                                {
                                    foreach (var kv in cfg.Headers)
                                    {
                                        var label = kv.Key;
                                        var value = kv.Value;

                                        // ✅ BỎ QUA Content-Type cho GET request cũng vậy
                                        if (string.Equals(label, "Content-Type", StringComparison.OrdinalIgnoreCase))
                                        {
                                            continue;
                                        }

                                        if (string.Equals(label, "Authorization", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var parts = value.Split(' ', 2);
                                            if (parts.Length == 2)
                                            {
                                                request.Headers.Authorization =
                                                    new AuthenticationHeaderValue(parts[0], parts[1]);
                                            }
                                            continue;
                                        }

                                        request.Headers.TryAddWithoutValidation(label, value);
                                    }
                                }

                                resp = httpClient.SendAsync(request).GetAwaiter().GetResult();
                            }
                            else
                                resp = httpClient.PostAsync(cfg.TargetUrl, content).GetAwaiter().GetResult();

                            try { respBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult(); } catch { respBody = ""; }

                            var parseRespBody = ParseJsonOrKeepString(respBody);

                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = new
                                {
                                    Url = cfg.TargetUrl,
                                    Method = cfg.Method,
                                    Headers = cfg.Headers,
                                    Body = requestBodyInfo
                                },
                                ResponseBody = new
                                {
                                    StatusCode = (int)resp.StatusCode,
                                    StatusDescription = resp.ReasonPhrase,
                                    Headers = resp.Headers.ToDictionary(h => h.Key, h => string.Join("; ", h.Value)),
                                    Body = parseRespBody
                                },
                                StepID = "Detail step - " + step.Id,
                                StepName = "Detail step - " + step.Description,
                                ErrorMessage = string.Empty,
                                WorkflowId = step.WfId,
                            });

                            // Log model
                            _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                            {
                                TenantId = tenantId,
                                Wfid = step.WfId,
                                WFName = wfName,
                                StepName = step.Description,
                                StepId = step.Id,
                                SiteRun = IBGlobalConfig.ThisSite,
                                KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                                Status = resp.IsSuccessStatusCode ? ExecuteRunApiThirdPartyStatus.Complete.ToString() : ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                                StatusCode = ((int)resp.StatusCode).ToString(),
                                Url = cfg.TargetUrl,
                                Request = SerializeHttpContentForLog(content).GetAwaiter().GetResult(),
                                Response = respBody,
                                Headers = JsonConvert.SerializeObject(cfg.Headers),
                                Reason = null,
                                CreatedDate = createDate,
                                ModificationDate = DateTime.Now
                            });

                            // xử lý response giống runCallAPI
                            // ===================== XỬ LÝ KHI API THÀNH CÔNG =====================
                            if (resp.IsSuccessStatusCode)
                            {
                                object? bindingdata02 = null;
                                if (!string.IsNullOrEmpty(respBody) && respBody.TrimStart().StartsWith("<"))
                                {
                                    bindingdata02 = this._modelControl.BindingDataXML(step.Response, respBody, tenantId);
                                }
                                else if (!string.IsNullOrEmpty(respBody) && respBody.TrimStart().StartsWith("["))
                                {
                                    JToken jToken = JToken.Parse(respBody);
                                    JToken wrapped = new JObject { ["data_convert_list_object"] = jToken };
                                    bindingdata02 = this._modelControl.BindingData(step.Response, JsonConvert.SerializeObject(wrapped), tenantId);
                                }
                                else
                                {
                                    bindingdata02 = this._modelControl.BindingData(step.Response, respBody, tenantId);
                                }

                                if (step.SaveResponseToCache ?? false)
                                {
                                    resp.Headers.ToList().ForEach(ptr =>
                                    {
                                        saveCahe($"{step.Id}_{ptr.Key}", new WFCache()
                                        {
                                            ObjType = typeof(string),
                                            Obj = ptr.Value.FirstOrDefault()
                                        });
                                    });

                                    saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                    {
                                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                                        Obj = bindingdata02
                                    });
                                }

                                // ✅ THÀNH CÔNG → Chạy childSteps THÔNG THƯỜNG
                                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                                return (bindingdata02, childSteps);
                            }
                            // ===================== XỬ LÝ KHI API LỖI (IsSuccessStatusCode = false) =====================
                            else
                            {
                                var configRestAPI = cfg;
                                var responseApi = respBody ?? string.Empty;
                                var statusCode = ((int)resp.StatusCode).ToString();

                                // Binding data error
                                object? bindingdataError = null;
                                if (!string.IsNullOrEmpty(responseApi) && responseApi.TrimStart().StartsWith("<"))
                                    bindingdataError = this._modelControl.BindingDataXML(step.Response, responseApi, tenantId);
                                else
                                    bindingdataError = this._modelControl.BindingData(step.Response, responseApi, tenantId);

                                // ✅ Kiểm tra xem có exception steps không
                                var hasExceptionSteps = step.ChildSteps?.Any(ptr => ptr.IsExceptionStep == true) ?? false;

                                if (!hasExceptionSteps)
                                {
                                    // ⚠️ KHÔNG CÓ EXCEPTION STEPS → Chạy childSteps thông thường
                                    if (step.SaveResponseToCache ?? false)
                                    {
                                        saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                        {
                                            ObjType = this._modelControl.GetType(step.Response, tenantId),
                                            Obj = bindingdataError
                                        });
                                    }

                                    var normalChildSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                                    return (bindingdataError, normalChildSteps);
                                }

                                // ✅ CÓ EXCEPTION STEPS → Tìm step phù hợp

                                // BƯỚC 1: Kiểm tra HttpCode mapping
                                if (configRestAPI?.Exception?.HttpCode != null && configRestAPI.Exception.HttpCode.Any())
                                {
                                    var matchedHttpCode = configRestAPI.Exception.HttpCode.FirstOrDefault(x => x.Code != null && x.Code == statusCode);

                                    if (matchedHttpCode != null && !string.IsNullOrEmpty(matchedHttpCode.StepId))
                                    {
                                        var stepExStatusCode = step.ChildSteps?.FirstOrDefault(ptr => ptr.IsExceptionStep == true && ptr.Id == matchedHttpCode.StepId);

                                        if (stepExStatusCode != null)
                                        {
                                            // Lưu cache nếu cần
                                            if (step.SaveResponseToCache ?? false)
                                            {
                                                saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                                {
                                                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                                                    Obj = bindingdataError
                                                });
                                            }

                                            // ✅ TÌM THẤY HttpCode mapping → Chạy exception step tương ứng
                                            return (bindingdataError ?? responseApi, new List<WFStep>() { stepExStatusCode });
                                        }
                                    }
                                }

                                // BƯỚC 2: Kiểm tra BodyResponse mapping
                                if (configRestAPI?.Exception?.BodyResponse != null && configRestAPI.Exception.BodyResponse.Any())
                                {
                                    try
                                    {
                                        var responseDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseApi, new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true
                                        });

                                        if (responseDict != null)
                                        {
                                            var matchedItem = configRestAPI.Exception.BodyResponse
                                                .FirstOrDefault(x => x.Value != null
                                                                  && responseDict.TryGetValue(x.Label, out var value)
                                                                  && value == x.Value);

                                            if (matchedItem != null && !string.IsNullOrEmpty(matchedItem.StepId))
                                            {
                                                var stepExBody = step.ChildSteps?.FirstOrDefault(ptr => ptr.IsExceptionStep == true && ptr.Id == matchedItem.StepId);

                                                if (stepExBody != null)
                                                {
                                                    // Lưu cache nếu cần
                                                    if (step.SaveResponseToCache ?? false)
                                                    {
                                                        saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                                        {
                                                            ObjType = this._modelControl.GetType(step.Response, tenantId),
                                                            Obj = bindingdataError
                                                        });
                                                    }

                                                    // ✅ TÌM THẤY BodyResponse mapping → Chạy exception step tương ứng
                                                    return (bindingdataError ?? responseApi, new List<WFStep>() { stepExBody });
                                                }
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Không parse được response body, bỏ qua BodyResponse check
                                    }
                                }

                                // BƯỚC 3: Không match được HttpCode hoặc BodyResponse → Lấy exception step KHÔNG được map
                                var allExceptionSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == true).ToList();

                                // Tìm các stepId đã được map trong config
                                HashSet<string> mappedStepIds = new HashSet<string>();

                                if (configRestAPI?.Exception?.HttpCode != null)
                                {
                                    foreach (var httpCode in configRestAPI.Exception.HttpCode)
                                    {
                                        if (!string.IsNullOrEmpty(httpCode.StepId))
                                            mappedStepIds.Add(httpCode.StepId);
                                    }
                                }

                                if (configRestAPI?.Exception?.BodyResponse != null)
                                {
                                    foreach (var bodyResp in configRestAPI.Exception.BodyResponse)
                                    {
                                        if (!string.IsNullOrEmpty(bodyResp.StepId))
                                            mappedStepIds.Add(bodyResp.StepId);
                                    }
                                }

                                // Tìm exception step KHÔNG nằm trong danh sách mapped (default exception step)
                                var defaultExceptionSteps = allExceptionSteps.Where(s => !mappedStepIds.Contains(s.Id)).ToList();

                                if (defaultExceptionSteps.Any())
                                {
                                    // Lưu cache nếu cần
                                    if (step.SaveResponseToCache ?? false)
                                    {
                                        saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                        {
                                            ObjType = this._modelControl.GetType(step.Response, tenantId),
                                            Obj = bindingdataError
                                        });
                                    }

                                    // ✅ CÓ DEFAULT EXCEPTION STEP → Chạy step đó
                                    return (bindingdataError, defaultExceptionSteps);
                                }

                                // ⚠️ TẤT CẢ exception steps đều được map cụ thể mà không match → Chạy childSteps thông thường
                                // Lưu cache nếu cần
                                if (step.SaveResponseToCache ?? false)
                                {
                                    saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                                    {
                                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                                        Obj = bindingdataError
                                    });
                                }

                                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                                return (bindingdataError, childSteps);
                            }
                        }
                        catch (Exception ex)
                        {
                            string requestBodyForLog = "";
                            try
                            {
                                requestBodyForLog = SerializeHttpContentForLog(content).GetAwaiter().GetResult();
                            }
                            catch
                            {
                                requestBodyForLog = "Unable to serialize request content";
                            }

                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = new
                                {
                                    Url = cfg.TargetUrl,
                                    Method = cfg.Method,
                                    Headers = cfg.Headers,
                                    Body = requestBodyForLog
                                },
                                ResponseBody = ParseJsonOrKeepString(respBody),
                                StepID = "Detail step - " + step.Id,
                                StepName = "Detail step - " + step.Description,
                                ErrorMessage = ex.Message,
                                WorkflowId = step.WfId,
                            });

                            _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, new APIThirdPartyModel()
                            {
                                TenantId = tenantId,
                                Wfid = step.WfId,
                                WFName = wfName,
                                StepName = step.Description,
                                StepId = step.Id,
                                SiteRun = IBGlobalConfig.ThisSite,
                                KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                                Status = ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                                StatusCode = "500",
                                Url = cfg.TargetUrl,
                                Request = SerializeHttpContentForLog(content).GetAwaiter().GetResult(),
                                Response = respBody,
                                Headers = JsonConvert.SerializeObject(cfg.Headers),
                                Reason = ex.Message,
                                CreatedDate = createDate,
                                ModificationDate = DateTime.Now
                            });

                            this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                            {
                                Id = tenantId,
                                MailTitle = "[Warning] Thông báo call API Lỗi trên Workflow",
                                MailBody = $"Exception {ex.Message}",
                                WFID = step.WfId,
                                StepID = step.Id,
                                typeWarning = TypeWarning.CallAPIError
                            });

                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Thông báo call API Lỗi trên Workflow",
                    MailBody = $"Exception {ex.Message}",
                    WFID = step.WfId,
                    StepID = step.Id,
                    typeWarning = TypeWarning.CallAPIError
                });

                throw;
            }
        }

        private string replaceTargetUrl(string targetUrl, object param, string tenantId)
        {
            try
            {
                var result = replaceProperty(targetUrl ?? string.Empty, string.Empty, param) ?? string.Empty;
                var urlTags = findTag(targetUrl ?? "");

                urlTags.ForEach(ptr =>
                {
                    if (string.IsNullOrEmpty(ptr)) return;
                    var objid = ptr.Split('.')[0];
                    
                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);

                    result = replacePropertyCache
                    (
                        objid,
                        result ?? string.Empty,
                        string.Empty, 
                        cacheEntry.Value?.Obj ?? new { }, 
                        tenantId
                    );
                });

                return result;
            }
            catch
            {
                return targetUrl ?? string.Empty;
            }
        }

        private object ExtractBodyInfoForLog(HttpContent content, string bodyType)
        {
            if (content is MultipartFormDataContent multipart)
            {
                var parts = new List<object>();

                // MultipartFormDataContent implement IEnumerable<HttpContent>
                // Có thể iterate trực tiếp mà không cần reflection
                try
                {
                    foreach (var part in multipart)
                    {
                        var contentDisposition = part.Headers.ContentDisposition;
                        var contentType = part.Headers.ContentType?.MediaType;

                        if (!string.IsNullOrEmpty(contentDisposition?.FileName))
                        {
                            // Đây là file
                            long? length = part.Headers.ContentLength;

                            parts.Add(new
                            {
                                Type = "file",
                                Name = contentDisposition.Name?.Trim('"'),
                                FileName = contentDisposition.FileName?.Trim('"'),
                                ContentType = contentType,
                                ContentLength = length,
                                ContentLengthReadable = length.HasValue ? $"{length.Value / 1024.0:F2} KB" : "Unknown"
                            });
                        }
                        else
                        {
                            string value = "";
                            try
                            {
                                // Đọc value từ stream (chỉ an toàn SAU KHI gửi request)
                                value = part.ReadAsStringAsync().GetAwaiter().GetResult();

                                // Truncate nếu quá dài
                                if (value.Length > 200)
                                    value = value.Substring(0, 200) + "...  (truncated)";
                            }
                            catch (Exception ex)
                            {
                                value = $"[Error reading: {ex.Message}]";
                            }

                            parts.Add(new
                            {
                                Type = "text",
                                Name = contentDisposition?.Name?.Trim('"'),
                                ContentType = contentType,
                                Value = value
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Nếu iterate fail, return error info
                    return new
                    {
                        BodyType = "form-data",
                        Error = $"Cannot iterate parts: {ex.Message}",
                        ContentType = content.Headers.ContentType?.ToString()
                    };
                }

                return new
                {
                    BodyType = "form-data",
                    Parts = parts,
                    TotalParts = parts.Count,
                    TotalFiles = parts.Count(p => ((dynamic)p).Type == "file"),
                    ContentType = content.Headers.ContentType?.ToString()
                };
            }
            else if (content is FormUrlEncodedContent)
            {
                string text = null;
                try
                {
                    text = content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }

                    var formData = ParseFormUrlEncoded(text);

                    var result = new Dictionary<string, object>();
                    foreach (var kvp in formData)
                    {
                        result[kvp.Key] = ParseJsonOrKeepString(kvp.Value);
                    }

                    return result;
                }
                catch
                {
                    return text;
                }
            }
            else if (content is StreamContent)
            {
                long? length = content.Headers.ContentLength;

                return new
                {
                    BodyType = "binary",
                    ContentType = content.Headers.ContentType?.MediaType,
                    ContentLength = length,
                    ContentLengthReadable = length.HasValue ? $"{length.Value / 1024.0:F2} KB" : "Unknown",
                    FileName = content.Headers.ContentDisposition?.FileName?.Trim('"')
                };
            }
            else if (content is StringContent || content is ByteArrayContent)
            {
                string text = null;
                try
                {
                    text = content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch
                {
                    text = null;
                }

                var parsedBody = ParseJsonOrKeepString(text);
                return parsedBody;
            }

            return new
            {
                BodyType = "unknown",
                ContentType = content.Headers.ContentType?.MediaType
            };
        }

        private static Dictionary<string, string> ParseFormUrlEncoded(string formData)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(formData))
            {
                return result;
            }

            var pairs = formData.Split('&');
            foreach (var pair in pairs)
            {
                var index = pair.IndexOf('=');
                if (index > 0)
                {
                    string key = Uri.UnescapeDataString(pair.Substring(0, index));
                    string value = Uri.UnescapeDataString(pair.Substring(index + 1));
                    result[key] = value;
                }
            }

            return result;
        }

        public static object ParseJsonOrKeepString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(input))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    return System.Text.Json.JsonSerializer.Deserialize<object>(input, options);
                }
            }
            catch (Exception)
            {
                return input;
            }
        }

        /// <summary>
        /// Parse Data URL format: data:name=screenshot. png;base64,iVBORw0KGgo... 
        /// </summary>
        private (bool success, string base64Data, string fileName) ParseDataUrl(string dataUrl)
        {
            if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:"))
            {
                return (false, null, null);
            }

            try
            {
                var parts = dataUrl.Split(new[] { ',' }, 2);
                if (parts.Length != 2)
                {
                    return (false, null, null);
                }

                string header = parts[0];
                string base64Data = parts[1];

                var headerParts = header.Split(';');

                string fileName = null;

                foreach (var part in headerParts)
                {
                    var trimmed = part.Trim();

                    if (trimmed == "data:" || trimmed.StartsWith("data:"))
                    {
                        if (trimmed.StartsWith("data:name="))
                        {
                            fileName = trimmed.Substring(10);
                        }
                        else if (trimmed.StartsWith("data:filename="))
                        {
                            fileName = trimmed.Substring(14);
                        }
                    }
                    else if (trimmed.StartsWith("name="))
                    {
                        fileName = trimmed.Substring(5);
                    }
                    else if (trimmed.StartsWith("filename="))
                    {
                        fileName = trimmed.Substring(9);
                    }
                }

                return (true, base64Data, fileName);
            }
            catch (Exception ex)
            {
                return (false, null, null);
            }
        }

        /// <summary>
        /// MIME types constants
        /// </summary>
        private static class MimeTypes
        {
            // Images
            public const string Png = "image/png";
            public const string Jpeg = "image/jpeg";
            public const string Gif = "image/gif";
            public const string Bmp = "image/bmp";
            public const string Webp = "image/webp";
            public const string Svg = "image/svg+xml";

            // Documents
            public const string Pdf = "application/pdf";
            public const string Docx = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            public const string Doc = "application/msword";
            public const string Xls = "application/vnd.ms-excel";
            public const string Ppt = "application/vnd.ms-powerpoint";

            // Archives
            public const string Zip = "application/zip";
            public const string Rar = "application/vnd.rar";
            public const string SevenZip = "application/x-7z-compressed";

            // Video
            public const string Mp4 = "video/mp4";
            public const string Avi = "video/x-msvideo";
            public const string Mpeg = "video/mpeg";
            public const string Webm = "video/webm";

            // Audio
            public const string Mp3 = "audio/mpeg";
            public const string Wav = "audio/wav";
            public const string Ogg = "audio/ogg";

            // Text
            public const string Plain = "text/plain";
            public const string Html = "text/html";
            public const string Css = "text/css";
            public const string Javascript = "text/javascript";
            public const string Json = "application/json";
            public const string Xml = "application/xml";

            // Default
            public const string OctetStream = "application/octet-stream";
        }

        /// <summary>
        /// Detect file type từ byte array (magic numbers)
        /// </summary>
        private (string contentType, string extension) DetectFileTypeFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return (MimeTypes.OctetStream, ".bin");

            // PNG
            if (bytes.Length >= 4 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return (MimeTypes.Png, ".png");

            // JPEG
            if (bytes.Length >= 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return (MimeTypes.Jpeg, ".jpg");

            // GIF
            if (bytes.Length >= 6 &&
                bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return (MimeTypes.Gif, ".gif");

            // PDF
            if (bytes.Length >= 5 &&
                bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return (MimeTypes.Pdf, ".pdf");

            // ZIP-based Office formats (DOCX, XLSX, PPTX)
            if (bytes.Length >= 4 &&
                bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
            {
                try
                {
                    using (var ms = new MemoryStream(bytes))
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false))
                    {
                        var entryNames = archive.Entries.Select(e => e.FullName.ToLower()).ToList();

                        // DOCX
                        if (entryNames.Any(name => name.StartsWith("word/")))
                        {
                            return (MimeTypes.Docx, ".docx");
                        }

                        // XLSX
                        if (entryNames.Any(name => name.StartsWith("xl/")))
                        {
                            return (MimeTypes.Xlsx, ".xlsx");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"[DetectFileType] Failed to parse ZIP: {ex.Message}", "");
                }

                // Default: ZIP
                return (MimeTypes.Zip, ".zip");
            }

            // Old Office formats (DOC, XLS, PPT) - Compound File Binary Format
            if (bytes.Length >= 8 &&
                bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0 &&
                bytes[4] == 0xA1 && bytes[5] == 0xB1 && bytes[6] == 0x1A && bytes[7] == 0xE1)
            {
                // Heuristic để phân biệt DOC/XLS/PPT
                if (bytes.Length > 2080)
                {
                    try
                    {
                        string signature = System.Text.Encoding.ASCII.GetString(bytes, 2080, Math.Min(100, bytes.Length - 2080));

                        // Excel
                        if (signature.Contains("Excel") || signature.Contains("Workbook"))
                        {
                            return (MimeTypes.Xls, ".xls");
                        }
                    }
                    catch { }
                }

                // Default: DOC
                return (MimeTypes.Doc, ".doc");
            }

            // WebP
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return (MimeTypes.Webp, ".webp");

            // RAR
            if (bytes.Length >= 7 &&
                bytes[0] == 0x52 && bytes[1] == 0x61 && bytes[2] == 0x72 && bytes[3] == 0x21 &&
                bytes[4] == 0x1A && bytes[5] == 0x07)
                return (MimeTypes.Rar, ".rar");

            // 7-Zip
            if (bytes.Length >= 6 &&
                bytes[0] == 0x37 && bytes[1] == 0x7A && bytes[2] == 0xBC && bytes[3] == 0xAF &&
                bytes[4] == 0x27 && bytes[5] == 0x1C)
                return (MimeTypes.SevenZip, ".7z");

            // MP4
            if (bytes.Length >= 12 &&
                bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
                return (MimeTypes.Mp4, ".mp4");

            // MP3
            if (bytes.Length >= 3 &&
                ((bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0) || // MPEG audio
                 (bytes[0] == 0x49 && bytes[1] == 0x44 && bytes[2] == 0x33))) // ID3
                return (MimeTypes.Mp3, ".mp3");

            // WAV
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x41 && bytes[10] == 0x56 && bytes[11] == 0x45)
                return (MimeTypes.Wav, ".wav");

            // Default
            return (MimeTypes.OctetStream, ".bin");
        }

        /// <summary>
        /// Kiểm tra xem string có phải base64 không
        /// </summary>
        private bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Loại bỏ whitespace
            value = value.Trim();

            // Base64 string length phải chia hết cho 4
            if (value.Length % 4 != 0)
                return false;

            // Kiểm tra độ dài tối thiểu (tránh detect string ngắn như "abc=" là base64)
            if (value.Length < 20)
                return false;

            // Base64 chỉ chứa A-Z, a-z, 0-9, +, /, =
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Za-z0-9+/]*={0,2}$"))
                return false;

            // Thử decode để chắc chắn
            try
            {
                byte[] data = Convert.FromBase64String(value);

                // Kiểm tra size hợp lý (ít nhất 10 bytes để là file)
                if (data.Length < 10)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private HttpContent BuildContentFromConfig(XRestAPIRequestWFConfig cfg, object param, IFormFileCollection? uploadedFiles, string tenantId)
        {
            var body = cfg.Body;
            if (body == null) return new StringContent("");

            var bodyType = (body.Type ?? "raw").ToLowerInvariant();

            switch (bodyType)
            {
                case "form-data":
                    MultipartFormDataContent multipart;

                    string customBoundary = null;
                    bool shouldAutoGenerateBoundary = false;

                    if (cfg.Headers != null && cfg.Headers.TryGetValue("Content-Type", out var customContentType))
                    {
                        if (string.IsNullOrWhiteSpace(customContentType) ||
                            customContentType.Contains("<calculated when request is sent>") ||
                            customContentType.Contains("boundary=<calculated") ||
                            string.Equals(customContentType.Trim(), "multipart/form-data", StringComparison.OrdinalIgnoreCase))
                        {
                            shouldAutoGenerateBoundary = true;
                        }
                        else
                        {
                            try
                            {
                                var parsed = MediaTypeHeaderValue.Parse(customContentType);
                                var boundaryParam = parsed.Parameters
                                    .FirstOrDefault(p => string.Equals(p.Name, "boundary", StringComparison.OrdinalIgnoreCase));

                                if (boundaryParam != null)
                                {
                                    customBoundary = boundaryParam.Value?.Trim('"');

                                    if (string.IsNullOrEmpty(customBoundary) ||
                                        customBoundary.Contains("<calculated"))
                                    {
                                        shouldAutoGenerateBoundary = true;
                                    }
                                }
                                else
                                {
                                    shouldAutoGenerateBoundary = true;
                                }
                            }
                            catch
                            {
                                shouldAutoGenerateBoundary = true;
                            }
                        }
                    }
                    else
                    {
                        shouldAutoGenerateBoundary = true;
                    }

                    if (shouldAutoGenerateBoundary || string.IsNullOrEmpty(customBoundary))
                    {
                        multipart = new MultipartFormDataContent(GeneratePostmanStyleBoundary());
                    }
                    else
                    {
                        multipart = new MultipartFormDataContent(customBoundary);
                    }

                    var textParts = new List<(string name, string value)>();
                    var fileParts = new List<(string name, IFormFile formFile)>();
                    var base64FileParts = new List<(string name, byte[] fileBytes, string fileName, string contentType)>();

                    var formFields = body.FormFields ?? new List<FormField>();

                    foreach (var ff in formFields)
                    {
                        var label = string.IsNullOrEmpty(ff.Key) ? (ff.Id ?? "field") : ff.Key;
                        var type = (ff.Type ?? "text").ToLowerInvariant();
                        var rawValue = ff.Value ?? "";

                        // XỬ LÝ FILE FIELDS
                        if (type == "file")
                        {
                            string valueStr = rawValue;

                            // Replace property placeholders
                            valueStr = replaceProperty(valueStr, "", param);

                            // Replace cache placeholders
                            var tags = findTag(valueStr);
                            if (tags != null && tags.Any())
                            {
                                foreach (var t in tags)
                                {
                                    if (string.IsNullOrEmpty(t)) continue;
                                    var objid = t.Split('.')[0];
                                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                    valueStr = replacePropertyCache(objid, valueStr, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                                }
                            }

                            // KIỂM TRA DATA URL FORMAT
                            if (valueStr.StartsWith("data:"))
                            {
                                var (success, base64Data, fileName) = ParseDataUrl(valueStr);

                                if (success)
                                {
                                    try
                                    {
                                        byte[] fileBytes = Convert.FromBase64String(base64Data);
                                        var (detectedContentType, extension) = DetectFileTypeFromBytes(fileBytes);

                                        // Nếu không có filename, generate
                                        if (string.IsNullOrWhiteSpace(fileName))
                                        {
                                            fileName = $"file_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                                        }
                                        else
                                        {
                                            // Đảm bảo có extension
                                            if (!Path.HasExtension(fileName))
                                            {
                                                fileName += extension;
                                            }
                                        }

                                        base64FileParts.Add((label, fileBytes, fileName, detectedContentType));
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new IboxLog($"[FORM-DATA] Failed to convert data URL in field '{label}': {ex.Message}", tenantId);
                                    }
                                }
                                else
                                {
                                    throw new IboxLog($"[FORM-DATA] Invalid data URL in field '{label}'", tenantId);
                                }
                            }
                            // ✅ KIỂM TRA PURE BASE64 (fallback)
                            else if (IsBase64String(valueStr))
                            {
                                try
                                {
                                    byte[] fileBytes = Convert.FromBase64String(valueStr);
                                    var (detectedContentType, extension) = DetectFileTypeFromBytes(fileBytes);

                                    string fileName = $"file_{DateTime.Now:yyyyMMddHHmmss}{extension}";

                                    base64FileParts.Add((label, fileBytes, fileName, detectedContentType));
                                }
                                catch (Exception ex)
                                {
                                    throw new IboxLog($"[FORM-DATA] Failed to convert base64 in field '{label}': {ex.Message}", tenantId);
                                }
                            }
                            else
                            {
                                throw new IboxLog($"[FORM-DATA] Field '{label}' type is 'file' but value is not valid data URL or base64", tenantId);
                            }
                        }
                        // XỬ LÝ TEXT FIELDS
                        else if (type == "text")
                        {
                            string valueStr = rawValue;

                            // Replace property placeholders
                            valueStr = replaceProperty(valueStr, "", param);

                            // Replace cache placeholders
                            var tags = findTag(valueStr);
                            if (tags != null && tags.Any())
                            {
                                foreach (var t in tags)
                                {
                                    if (string.IsNullOrEmpty(t)) continue;
                                    var objid = t.Split('.')[0];
                                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                    valueStr = replacePropertyCache(objid, valueStr, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                                }
                            }

                            textParts.Add((label, valueStr));
                        }
                    }

                    // Lấy uploaded files từ request (nếu có)
                    if (uploadedFiles != null && uploadedFiles.Any())
                    {
                        // Tìm field name cho uploaded files
                        var uploadFileFieldConfig = formFields
                            .FirstOrDefault(f => string.Equals(f.Type, "file", StringComparison.OrdinalIgnoreCase));

                        string uploadFileFieldName = uploadFileFieldConfig?.Key ??
                                                     uploadFileFieldConfig?.Id ??
                                                     "file";

                        foreach (var uploadedFile in uploadedFiles)
                        {
                            fileParts.Add((uploadFileFieldName, uploadedFile));
                        }
                    }

                    // Add text parts
                    foreach (var t in textParts)
                    {
                        multipart.Add(new StringContent(t.value ?? ""), t.name);
                    }

                    // Add base64 file parts (từ field type="file" với value là base64/data URL)
                    foreach (var bf in base64FileParts)
                    {
                        var byteContent = new ByteArrayContent(bf.fileBytes);
                        byteContent.Headers.ContentType = new MediaTypeHeaderValue(bf.contentType);
                        multipart.Add(byteContent, bf.name, bf.fileName);
                    }

                    // Add uploaded file parts (từ IFormFile collection)
                    foreach (var fp in fileParts)
                    {
                        var formFile = fp.formFile;

                        var stream = formFile.OpenReadStream();
                        var streamContent = new StreamContent(stream);

                        if (!string.IsNullOrEmpty(formFile.ContentType))
                        {
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
                        }
                        else
                        {
                            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        }

                        multipart.Add(streamContent, fp.name, formFile.FileName);
                    }

                    return multipart;

                case "x-www-form-urlencoded":
                    var pairs = new List<KeyValuePair<string, string>>();

                    if (body.UrlEncodedFields != null)
                    {
                        foreach (var ff in body.UrlEncodedFields)
                        {
                            var fieldName = ff.Key ?? ff.Id ?? "field";
                            var rawValue = ff.Value ?? "";
                            rawValue = replaceProperty(rawValue, "", param);

                            var tags = findTag(rawValue);
                            foreach (var t in tags)
                            {
                                if (string.IsNullOrEmpty(t)) continue;
                                var objid = t.Split('.')[0];
                                var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                                rawValue = replacePropertyCache(objid, rawValue, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                            }

                            pairs.Add(new KeyValuePair<string, string>(fieldName, rawValue));
                        }
                    }

                    if (param is IDictionary<string, object> p2)
                    {
                        foreach (var kv in p2)
                        {
                            if (kv.Key.Equals("__files", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (!pairs.Any(x => x.Key == kv.Key))
                                pairs.Add(new KeyValuePair<string, string>(kv.Key, kv.Value?.ToString() ?? ""));
                        }
                    }

                    var formUrlContent = new FormUrlEncodedContent(pairs);
                    // Gán content-type rõ ràng để log/khớp type chính xác
                    try
                    {
                        formUrlContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                    }
                    catch { /* ignore */ }

                    return formUrlContent;

                case "binary":
                    // ưu tiên file trong param.__files hoặc uploadedFiles; nếu không có dùng body.BinaryValue base64
                    IFormFile? fileToSend = null;
                    if (param is IDictionary<string, object> dictParam3 && dictParam3.ContainsKey("__files") && dictParam3["__files"] is IFormFileCollection coll2 && coll2.Count > 0)
                    {
                        fileToSend = coll2.FirstOrDefault();
                    }

                    if (fileToSend == null && uploadedFiles != null && uploadedFiles.Count > 0)
                    {
                        fileToSend = uploadedFiles.FirstOrDefault();
                    }

                    if (fileToSend != null)
                    {
                        var sc = new StreamContent(fileToSend.OpenReadStream());
                        sc.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                        {
                            FileName = $"\"{fileToSend.FileName}\""
                        };
                        // đảm bảo Content-Type
                        sc.Headers.ContentType = new MediaTypeHeaderValue(fileToSend.ContentType ?? "application/octet-stream");
                        return sc;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(body.BinaryValue))
                        {
                            try
                            {
                                var bytes = Convert.FromBase64String(body.BinaryValue);
                                var bc = new ByteArrayContent(bytes);
                                bc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                return bc;
                            }
                            catch
                            {
                                return new StringContent(body.BinaryValue);
                            }
                        }
                        return new ByteArrayContent(new byte[0]);
                    }

                case "raw":
                default:
                    var raw = body.RawContent ?? "";
                    raw = replaceProperty(raw, "", param);
                    var tagsRaw = findTag(raw);
                    foreach (var t in tagsRaw)
                    {
                        if (string.IsNullOrEmpty(t)) continue;
                        var objid = t.Split('.')[0];
                        var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                        raw = replacePropertyCache(objid, raw, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                    }

                    string contentType = cfg.Headers.FirstOrDefault(ptr => ptr.Key?.ToLower() == "content-type".ToLower()).Value;

                    if (contentType == "application/json")
                    {
                        raw = SanitizeJson(raw);
                    }

                    var scRaw = new StringContent(raw ?? "", Encoding.UTF8, contentType);

                    return scRaw;
            }
        }

        private string GeneratePostmanStyleBoundary()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(100000000, 999999999);
            return $"--------------------------{timestamp}{random}";
        }

        private string SanitizeJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return json;
            }
        }

        private string FormatJsonStringIfPossible(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return json;
            }
        }

        private static async Task<string> SerializeHttpContentForLog(HttpContent content)
        {
            if (content == null) return "(null)";
            try
            {
                if (content is MultipartFormDataContent multipart)
                {
                    // Reuse SerializeMultipartContentToJson từ file (nếu có), else simple serialize
                    return await SerializeMultipartContentToJson(multipart);
                }
                else
                {
                    return await content.ReadAsStringAsync();
                }
            }
            catch
            {
                return "(unserializable content)";
            }
        }
        
        private (dynamic, List<WFStep>) runScript(WFStep step, object param, IFormFileCollection? uploadedFiles, string wfName, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog($"Can not found Config in step {step.Id}", tenantId, "Error");
                }

                var cfg = JsonConvert.DeserializeObject<ScriptConfig>(step.Config) ?? new ScriptConfig();

                // Bind param according to project's pattern
                var boundParam = this._modelControl.BindingData(step.Param, param, tenantId);


                const int MAX_CODE_LEN = 15000;
                if (!string.IsNullOrEmpty(cfg.Code) && cfg.Code.Length > MAX_CODE_LEN)
                {
                    throw new IboxLog("Script code too long.", tenantId, "Error");
                }

                SecurityValidateOrThrow(cfg.Code ?? "", tenantId);

                // forbid dangerous patterns
                if (cfg.Code != null && (cfg.Code.Contains("new Function") || cfg.Code.Contains("Function(")))
                {
                    throw new IboxLog("Prohibited script patterns.", tenantId, "Error");
                }

                // 1) replace param placeholders if any
                if (!string.IsNullOrEmpty(cfg.Code))
                {
                    cfg.Code = replaceProperty(cfg.Code, "", boundParam);
                }

                // 2) find tags like [<objid>.<prop>] and replace from caches
                if (!string.IsNullOrEmpty(cfg.Code) && cfg.Code.Contains('['))
                {
                    var listTags = findTag(cfg.Code);
                    
                    foreach (var tag in listTags)
                    {
                        if (string.IsNullOrEmpty(tag)) continue;

                        // tag có dạng "objid.property" hoặc có thể "objid" (không có .)
                        var parts = tag.Split('.', 2);
                        var objid = parts.FirstOrDefault();
                        var fieldName = parts.Length > 1 ? parts[1] : "";

                        if (string.IsNullOrEmpty(objid)) continue;

                        var cacheEntry = this.caches.FirstOrDefault(x => x.Key == objid);
                        if (cacheEntry.Value != null)
                        {
                            // truyền fieldName thay vì "" để replacePropertyCache có thể xử lý các pattern ->fielName nếu có
                            cfg.Code = replacePropertyCache(objid, cfg.Code, fieldName ?? "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                        }
                    }
                }
                
                object resultObj = null;

                if ((cfg.Language ?? "js").ToLower() == "js")
                {
                    // Convert boundParam to a dynamic/Expando so Jint sees plain POJO-like object
                    var paramJson = JsonConvert.SerializeObject(boundParam);
                    var paramExpando = JsonConvert.DeserializeObject<ExpandoObject>(paramJson);

                    // Prepare a simple cache view (shallow) to expose to script (if needed)
                    var cacheView = new Dictionary<string, object>();
                    if (this.caches != null)
                    {
                        foreach (var kv in this.caches)
                        {
                            cacheView[kv.Key] = kv.Value?.Obj;
                        }
                    }

                    // Create engine WITHOUT calling option methods that may not exist on some Jint versions
                    var engine = new Jint.Engine();

                    engine.SetValue("eval", Jint.Native.JsValue.Undefined);
                    engine.SetValue("Function", Jint.Native.JsValue.Undefined);
                    engine.SetValue("fetch", Jint.Native.JsValue.Undefined);
                    engine.SetValue("XMLHttpRequest", Jint.Native.JsValue.Undefined);
                    engine.SetValue("ActiveXObject", Jint.Native.JsValue.Undefined);
                    engine.SetValue("axios", Jint.Native.JsValue.Undefined);
                    engine.SetValue("require", Jint.Native.JsValue.Undefined);
                    engine.SetValue("module", Jint.Native.JsValue.Undefined);
                    engine.SetValue("exports", Jint.Native.JsValue.Undefined);
                    engine.SetValue("process", Jint.Native.JsValue.Undefined);
                    engine.SetValue("global", Jint.Native.JsValue.Undefined);
                    engine.SetValue("globalThis", Jint.Native.JsValue.Undefined);
                    engine.SetValue("WebAssembly", Jint.Native.JsValue.Undefined);
                    engine.SetValue("SharedArrayBuffer", Jint.Native.JsValue.Undefined);
                    engine.SetValue("Atomics", Jint.Native.JsValue.Undefined);
                    engine.SetValue("Buffer", Jint.Native.JsValue.Undefined);

                    // Expose param and cache
                    engine.SetValue("param", paramExpando);
                    engine.SetValue("cache", cacheView);

                    // Expose uploaded files metadata only (no streams)
                    if (uploadedFiles != null && uploadedFiles.Count > 0)
                    {
                        var filesMeta = uploadedFiles.Select(f => new { f.Name, f.FileName, f.Length }).ToArray();
                        engine.SetValue("__uploadedFilesMeta", filesMeta);
                    }

                    // Wrap user's code; assign result into a known global var __jint_internal_result
                    var wrapper = $@"
                                    var __jint_internal_result = (function() {{
                                        try {{
                                            {cfg.Code}
                                            if (typeof run === 'function') return run(param, cache);
                                            if (typeof main === 'function') return main(param, cache);
                                            return null;
                                        }} catch(e) {{
                                            throw e;
                                        }}
                                    }})();
                                    ";

                    // Execute engine inside Task with watchdog for timeout
                    var cts = new CancellationTokenSource();
                    var token = cts.Token;
                    var timeoutMs = Math.Clamp(cfg.TimeoutMs, 1000, 30000);

                    var execTask = Task.Run(() =>
                    {
                        engine.Execute(wrapper);
                        var jsVal = engine.GetValue("__jint_internal_result");
                        return jsVal.IsUndefined() ? null : jsVal.ToObject();
                    }, token);

                    bool finished = execTask.Wait(TimeSpan.FromMilliseconds(timeoutMs));
                    if (!finished)
                    {
                        try { cts.Cancel(); } catch { }
                        throw new TimeoutException($"Script execution exceeded {timeoutMs} ms");
                    }

                    resultObj = execTask.Result;
                }
                else
                {
                    throw new NotSupportedException($"Script language '{cfg.Language}' is not supported.");
                }

                // Bind result to response model (same pattern used by other run* methods)
                var resultJson = JsonConvert.SerializeObject(resultObj);
                var bindingData = this._modelControl.BindingData(step.Response, resultJson, tenantId);

                // Save to cache if requested
                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId, "Error"), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingData
                    });
                }

                // Save debug
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = cfg.Code,
                    ResponseBody = bindingData,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();
                return (bindingData, childSteps);
            }
            catch (Exception ex)
            {
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = "",
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = ex.Message,
                    WorkflowId = step.WfId
                });
                throw;
            }
        }

        private static string DecodeUnicodeEscapes(string s)
        {
            return RxUnicodeEscape.Replace(s, m =>
            {
                var hex = m.Groups[1].Value;
                var ch = (char)Convert.ToInt32(hex, 16);
                return ch.ToString();
            });
        }

        private void SecurityValidateOrThrow(string code, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new IboxLog("Script is empty.", tenantId, "Error");

            // Chuẩn hoá unicode escapes để tránh lách luật (vd: \u0072equire)
            var normalized = DecodeUnicodeEscapes(code);
            var lowered = normalized.ToLowerInvariant();

            // 0) Fast-fail: pattern nhanh
            string[] bannedSubstr = {
                "eval(", "new function", "function(",       // function( cho trường hợp new Function bị viết méo mó
                "import(", "require(",
                "xmlhttprequest", "fetch(", "activexobject", "axios",
                "globalthis", "process", "window", "document",
                "webassembly", "sharedarraybuffer", "atomics", "buffer",
                "proxy", "reflect",
                "__proto__", "constructor", "prototype",
                "__definegetter__", "__definesetter__"
            };

            foreach (var bad in bannedSubstr)
            {
                if (lowered.Contains(bad))
                {
                    throw new IboxLog($"Phát hiện cú pháp không được sử dụng: {bad}", tenantId, "Error");
                }
            }

            if (RxInfiniteWhile.IsMatch(normalized) || RxInfiniteFor.IsMatch(normalized))
            {
                throw new IboxLog("Phát hiện vòng lặp vô hạn.", tenantId, "Error");
            }

            // 1) Parse AST
            Program ast;
            try
            {
                var parser = new JavaScriptParser(new ParserOptions { Tolerant = true });
                ast = parser.ParseScript(normalized);
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Invalid JavaScript: {ex.Message}", tenantId, "Error");
            }

            // 2) Duyệt AST
            var bannedIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // động code
                "eval", "Function",

                // network/web
                "fetch", "XMLHttpRequest", "ActiveXObject", "axios",

                // module/bridge
                "require", "module", "exports", "import",

                // process-like / memory primitives
                "process", "global", "globalThis", "window", "document",
                "WebAssembly", "SharedArrayBuffer", "Atomics", "Buffer",

                // meta-programming có thể lách sandbox
                "Proxy", "Reflect"
            };

                    var bannedPropNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "__proto__", "constructor", "prototype",
                "__defineGetter__", "__defineSetter__"
            };

                    var bannedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "fs","http","https","net","tls","dgram","child_process","worker_threads","vm",
                "node:fs","node:http","node:https","node:child_process","node:vm"
            };

            int runDeclCount = 0;

            void Ban(string msg) => throw new IboxLog(msg, tenantId, "Error");

            void Walk(Node node)
            {
                switch (node)
                {
                    // Đếm run()
                    case FunctionDeclaration fd:
                        if (fd.Id != null && fd.Id.Name == "run") runDeclCount++;
                        break;
                    case VariableDeclarator vd:
                        if (vd.Id is Identifier vid && vid.Name == "run")
                        {
                            if (vd.Init is FunctionExpression || vd.Init is ArrowFunctionExpression)
                                runDeclCount++;
                        }
                        break;

                    // Import ESM
                    case ImportDeclaration imp:
                        if (imp.Source?.Value is string im && !string.IsNullOrEmpty(im))
                        {
                            if (bannedModules.Contains(im)) Ban($"Importing banned module: {im}");
                            Ban("ES module import is prohibited in sandbox.");
                        }
                        break;

                    // Lời gọi nguy hiểm
                    case CallExpression call:
                        {
                            // require('fs'), import('fs'), axios(...), axios.get(...)
                            if (call.Callee is Identifier id)
                            {
                                var name = id.Name;
                                if (bannedIdentifiers.Contains(name))
                                {
                                    if (name.Equals("require", StringComparison.OrdinalIgnoreCase) ||
                                        name.Equals("import", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (call.Arguments.Count > 0 && call.Arguments[0] is Literal l && l.Value is string mod)
                                        {
                                            if (bannedModules.Contains(mod))
                                                Ban($"Requiring banned module: {mod}");
                                        }
                                        Ban("Module loading is prohibited in sandbox.");
                                    }
                                    else
                                    {
                                        Ban($"Không được gọi: {name}()");
                                    }
                                }
                            }

                            if (call.Callee is MemberExpression me)
                            {
                                // axios.get / axios['post']
                                if (me.Object is Identifier objId && objId.Name.Equals("axios", StringComparison.OrdinalIgnoreCase))
                                    Ban("HTTP calls via axios are prohibited.");

                                // globalThis/window . eval/Function
                                if (me.Object is Identifier gobj && (gobj.Name == "globalThis" || gobj.Name == "window"))
                                {
                                    if (!me.Computed && me.Property is Identifier pid &&
                                        (pid.Name == "eval" || pid.Name == "Function"))
                                        Ban("Dynamic code via globalThis/window is prohibited.");
                                    if (me.Computed && me.Property is Literal lit && lit.Value is string s &&
                                        (s == "eval" || s == "Function"))
                                        Ban("Dynamic code via globalThis/window is prohibited.");
                                }

                                // Thuộc tính nguy hiểm bất kể object là gì
                                string? propName = null;
                                if (!me.Computed && me.Property is Identifier pid2) propName = pid2.Name;
                                else if (me.Computed && me.Property is Literal lit2 && lit2.Value is string s2) propName = s2;

                                if (propName != null && bannedPropNames.Contains(propName))
                                    Ban($"Access to dangerous property: {propName}");
                            }
                            break;
                        }

                    // new Function(...)
                    case NewExpression ne:
                        if (ne.Callee is Identifier nid && nid.Name == "Function")
                            Ban("Forbidden: new Function(...)");
                        break;

                    // Truy cập identifier nguy hiểm
                    case Identifier ident:
                        if (bannedIdentifiers.Contains(ident.Name))
                            Ban($"Forbidden identifier: {ident.Name}");
                        break;

                    // Truy cập property nguy hiểm (obj.constructor / obj['__proto__'])
                    case MemberExpression me2:
                        {
                            string? propName = null;
                            if (!me2.Computed && me2.Property is Identifier p1) propName = p1.Name;
                            else if (me2.Computed && me2.Property is Literal p2 && p2.Value is string s) propName = s;

                            if (propName != null && bannedPropNames.Contains(propName))
                                Ban($"Access to dangerous property: {propName}");
                            break;
                        }
                }

                foreach (var child in node.ChildNodes)
                    Walk(child);
            }

            Walk(ast);

            if (runDeclCount == 0)
            {
                throw new IboxLog("Script must define function run(...).", tenantId, "Error");
            }
            
            if (!Regex.IsMatch(normalized, @"return\s*\{", RegexOptions.Singleline))
            {
                throw new IboxLog("Function run must return an object (return { ... }).", tenantId, "Error");
            }
        }

        // Helper: gửi request (POST/PUT), log theo 3 dạng: Success(200), Fail(!=200), Error(exception)
        private HttpResponseMessage SendRequestAndLog(HttpClient httpClient,
                                                     UploadFileConfig config,
                                                     string targetUrl,
                                                     HttpContent content,
                                                     string tenantId,
                                                     WFStep step,
                                                     string wfName)
        {
            HttpResponseMessage resp = null;
            string keyExecuteRunApiThirdParty = Guid.NewGuid().ToString();
            DateTime createDate = DateTime.Now;
            // 💡 Serialize nội dung MultipartFormDataContent ra JSON để log
            string requestJson = "(unavailable)";
            try
            {
                if (content is MultipartFormDataContent multipart)
                {
                    requestJson = SerializeMultipartContentToJson(multipart).GetAwaiter().GetResult();
                }
                else
                {
                    requestJson = content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            }
            catch
            {
                requestJson = "(error serializing content)";
            }

            try
            {
                // Gửi request (POST hoặc PUT)
                var method = string.IsNullOrEmpty(config.Method) ? "POST" : config.Method.ToUpperInvariant();
                if (method == "PUT")
                    resp = httpClient.PutAsync(targetUrl, content).GetAwaiter().GetResult();
                else
                    resp = httpClient.PostAsync(targetUrl, content).GetAwaiter().GetResult();

                // Đọc body phản hồi
                string respBody;
                try { respBody = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult(); }
                catch { respBody = string.Empty; }

                

                // Chuẩn bị model log
                var model = new APIThirdPartyModel()
                {
                    TenantId = tenantId,
                    Wfid = step.WfId,
                    WFName = wfName,
                    StepName = step.Description,
                    StepId = step.Id,
                    SiteRun = IBGlobalConfig.ThisSite,
                    KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                    StatusCode = resp != null ? ((int)resp.StatusCode).ToString() : "N/A",
                    Url = config.TargetUrl,
                    Request = requestJson, // <-- JSON form-data
                    Headers = JsonConvert.SerializeObject(config.Headers),
                    ModificationDate = DateTime.Now,
                    CreatedDate = createDate
                };

                // Ghi log theo trạng thái
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    model.Status = ExecuteRunApiThirdPartyStatus.Complete.ToString();
                    model.Response = respBody;
                    model.Reason = null;
                }
                else
                {
                    model.Status = ExecuteRunApiThirdPartyStatus.Fail.ToString();
                    model.Response = null;
                    model.Reason = respBody;
                }

                _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, model);
                return resp;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi (Error)
                try
                {
                    var modelErr = new APIThirdPartyModel()
                    {
                        TenantId = tenantId,
                        Wfid = step.WfId,
                        WFName = wfName,
                        StepName = step.Description,
                        StepId = step.Id,
                        SiteRun = IBGlobalConfig.ThisSite,
                        KeyExecuteRunApiThirdParty = keyExecuteRunApiThirdParty,
                        Status = ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                        StatusCode = resp != null ? ((int)resp.StatusCode).ToString() : "N/A",
                        Url = config.TargetUrl,
                        Request = requestJson,
                        Response = "Call api exception:" + ex.Message,
                        Headers = JsonConvert.SerializeObject(config.Headers),
                        Reason = ex.Message,
                        ModificationDate = DateTime.Now,
                        CreatedDate = createDate
                    };
                    _restLog.WriteFileLogAPI(IBGlobalConfig.ServiceName, modelErr);
                }
                catch { /* tránh lỗi khi log lỗi */ }

                throw;
            }
        }

        private static async Task<string> SerializeMultipartContentToJson(MultipartFormDataContent content)
        {
            var textFields = new Dictionary<string, string>();
            var fileFields = new List<object>();

            foreach (var part in content)
            {
                var cd = part.Headers.ContentDisposition;
                var name = cd?.Name?.Trim('"') ?? "(unknown)";
                var fileName = cd?.FileName?.Trim('"');

                if (!string.IsNullOrEmpty(fileName))
                {
                    string contentType = part.Headers.ContentType?.ToString() ?? "application/octet-stream";
                    long? length = null;

                    if (part.Headers.ContentLength.HasValue)
                        length = part.Headers.ContentLength.Value;
                    else if (part is StreamContent sc)
                    {
                        try
                        {
                            if (sc.Headers.ContentLength.HasValue)
                                length = sc.Headers.ContentLength.Value;
                        }
                        catch { }
                    }

                    fileFields.Add(new
                    {
                        FieldName = name,
                        FileName = fileName,
                        ContentType = contentType,
                        Size = length ?? 0
                    });
                }
                else
                {
                    string value;
                    try
                    {
                        value = await part.ReadAsStringAsync();
                    }
                    catch
                    {
                        value = "(unreadable)";
                    }

                    textFields[name] = value;
                }
            }

            var result = new
            {
                TextFields = textFields,
                FileFields = fileFields
            };

            // <-- dùng JsonConvert không có Formatting.Indented
            return JsonConvert.SerializeObject(result);
        }

        /// <summary>
        /// Call dll xử lý đặc thù
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runCallDLL(WFStep step, object param, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.DllID))
                {
                    throw new IboxLog("Can not found DLL", tenantId);
                }

                if (string.IsNullOrEmpty(step.FunctionName))
                {
                    throw new IboxLog("Can not found FunctionName", tenantId);
                }

                var response1 = runDLL(step, step.DllID, this._modelControl.BindingData(step.Param, param, tenantId), step.FunctionName, tenantId);
                var bindingdata01 = this._modelControl.BindingData(step.Response, response1, tenantId);
                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingdata01
                    });
                }
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingdata01,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var ChildSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingdata01, ChildSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Call API đến các dịch vụ bên thứ 3 bao gồm core data và dịch vụ các các đơn vị khác
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="wfName"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runCallAPI(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog($"Can not found Config in step {step.Id}", tenantId);
                }

                RestAPIResponse response2 = callRestAPI(step.Config, this._modelControl.BindingData(step.Param, param, tenantId), step, wfName, tenantId);

                if (response2 == null)
                {
                    throw new IboxLog($"Call API Step has an error in step {step.Id}", tenantId);
                }

                HttpStatusCode[] allStatusCodes = (HttpStatusCode[])Enum.GetValues(typeof(HttpStatusCode));
                var lstStatusCode = allStatusCodes.Where(code => ((int)code >= 200 && (int)code < 300)).ToList();

                if (lstStatusCode != null && !lstStatusCode.Contains(response2.HttpResponse.StatusCode))
                {
                    this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = tenantId,
                        MailTitle = "[Warning] Thông báo call API Lỗi trên Workflow! ",
                        MailBody = $"Status Code: {response2.HttpResponse.StatusCode}<BR>Response: {response2.Result}",
                        WFID = step.WfId,
                        StepID = step.Id,
                        typeWarning = TypeWarning.CallAPIError
                    });
                }

                // ===================== XỬ LÝ KHI API THÀNH CÔNG =====================
                if (response2.HttpResponse?.StatusCode == HttpStatusCode.OK)
                {
                    var configRestAPI = JsonConvert.DeserializeObject<RestAPIRequestWFConfig>(step.Config);
                    object? bindingdata02 = null;

                    // Check kiểm tra xml hay Json để convert
                    if (!string.IsNullOrEmpty(response2.Result) && response2.Result.FirstOrDefault() == '<')
                    {
                        bindingdata02 = this._modelControl.BindingDataXML(step.Response, response2.Result, tenantId);
                    }
                    else if (!string.IsNullOrEmpty(response2.Result) && response2.Result.TrimStart().StartsWith("["))
                    {
                        JToken jToken = JToken.Parse(response2.Result);
                        JToken wrapped = new JObject
                        {
                            ["data_convert_list_object"] = jToken
                        };
                        bindingdata02 = this._modelControl.BindingData(step.Response, JsonConvert.SerializeObject(wrapped), tenantId);
                    }
                    else
                    {
                        bindingdata02 = this._modelControl.BindingData(step.Response, response2.Result, tenantId);
                    }

                    // Lưu cache nếu cần
                    if (step.SaveResponseToCache ?? false)
                    {
                        response2.HttpResponse.Headers.ToList().ForEach(ptr =>
                        {
                            saveCahe($"{step.Id}_{ptr.Key}", new WFCache()
                            {
                                ObjType = typeof(string),
                                Obj = ptr.Value.FirstOrDefault()
                            });
                        });

                        saveCahe(step.Id ?? throw new IboxLog("Step id is null", tenantId), new WFCache()
                        {
                            ObjType = this._modelControl.GetType(step.Response, tenantId),
                            Obj = bindingdata02
                        });
                    }

                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = bindingdata02,
                        StepID = step.Id,
                        StepName = step.Description,
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });

                    // ✅ THÀNH CÔNG → Chạy childSteps THÔNG THƯỜNG
                    var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                    return (bindingdata02, childSteps);
                }
                // ===================== XỬ LÝ KHI API LỖI (StatusCode != 200) =====================
                else
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = response2.HttpResponse?.StatusCode,
                        StepID = step.Id,
                        StepName = step.Description,
                        ErrorMessage = response2.HttpResponse?.ReasonPhrase,
                        WorkflowId = step.WfId
                    });

                    var configRestAPI = JsonConvert.DeserializeObject<RestAPIRequestWFConfig>(step.Config);
                    var responseApi = response2.Result?.ToString() ?? string.Empty;
                    var statusCode = ((int?)response2.HttpResponse?.StatusCode).ToString();

                    // Binding data error
                    object? bindingdataError = null;
                    if (response2 != null && !string.IsNullOrEmpty(response2.Result) && response2.Result.FirstOrDefault() == '<')
                    {
                        bindingdataError = this._modelControl.BindingDataXML(step.Response, response2.Result, tenantId);
                    }
                    else
                    {
                        bindingdataError = this._modelControl.BindingData(step.Response, response2.Result, tenantId);
                    }

                    // ✅ Kiểm tra xem có exception steps không
                    var hasExceptionSteps = step.ChildSteps?.Any(ptr => ptr.IsExceptionStep == true) ?? false;

                    if (!hasExceptionSteps)
                    {
                        // ⚠ KHÔNG CÓ EXCEPTION STEPS → Chạy childSteps thông thường
                        var normalChildSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                        return (bindingdataError, normalChildSteps);
                    }

                    // ✅ CÓ EXCEPTION STEPS → Tìm step phù hợp

                    // BƯỚC 1: Kiểm tra HttpCode mapping
                    if (configRestAPI?.Exception?.HttpCode != null && configRestAPI.Exception.HttpCode.Any())
                    {
                        var matchedHttpCode = configRestAPI.Exception.HttpCode.FirstOrDefault(x => x.Code != null && x.Code == statusCode);

                        if (matchedHttpCode != null && !string.IsNullOrEmpty(matchedHttpCode.StepId))
                        {
                            var stepExStatusCode = step.ChildSteps?.FirstOrDefault(ptr => ptr.IsExceptionStep == true && ptr.Id == matchedHttpCode.StepId);

                            if (stepExStatusCode != null)
                            {
                                // ✅ TÌM THẤY HttpCode mapping → Chạy exception step tương ứng
                                return (bindingdataError ?? responseApi, new List<WFStep>() { stepExStatusCode });
                            }
                        }
                    }

                    // BƯỚC 2: Kiểm tra BodyResponse mapping
                    if (configRestAPI?.Exception?.BodyResponse != null && configRestAPI.Exception.BodyResponse.Any())
                    {
                        try
                        {
                            var responseDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(responseApi, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (responseDict != null)
                            {
                                var matchedItem = configRestAPI.Exception.BodyResponse
                                    .FirstOrDefault(x => x.Value != null
                                                      && responseDict.TryGetValue(x.Label, out var value)
                                                      && value == x.Value);

                                if (matchedItem != null && !string.IsNullOrEmpty(matchedItem.StepId))
                                {
                                    var stepExBody = step.ChildSteps?.FirstOrDefault(ptr => ptr.IsExceptionStep == true && ptr.Id == matchedItem.StepId);

                                    if (stepExBody != null)
                                    {
                                        // ✅ TÌM THẤY BodyResponse mapping → Chạy exception step tương ứng
                                        return (bindingdataError ?? responseApi, new List<WFStep>() { stepExBody });
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Không parse được response body, bỏ qua BodyResponse check
                        }
                    }

                    // BƯỚC 3: Không match được HttpCode hoặc BodyResponse → Lấy exception step KHÔNG được map
                    var allExceptionSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == true).ToList();

                    // Tìm các stepId đã được map trong config
                    HashSet<string> mappedStepIds = new HashSet<string>();

                    if (configRestAPI?.Exception?.HttpCode != null)
                    {
                        foreach (var httpCode in configRestAPI.Exception.HttpCode)
                        {
                            if (!string.IsNullOrEmpty(httpCode.StepId))
                                mappedStepIds.Add(httpCode.StepId);
                        }
                    }

                    if (configRestAPI?.Exception?.BodyResponse != null)
                    {
                        foreach (var bodyResp in configRestAPI.Exception.BodyResponse)
                        {
                            if (!string.IsNullOrEmpty(bodyResp.StepId))
                                mappedStepIds.Add(bodyResp.StepId);
                        }
                    }

                    // Tìm exception step KHÔNG nằm trong danh sách mapped (default exception step)
                    var defaultExceptionSteps = allExceptionSteps.Where(s => !mappedStepIds.Contains(s.Id)).ToList();

                    if (defaultExceptionSteps.Any())
                    {
                        // ✅ CÓ DEFAULT EXCEPTION STEP → Chạy step đó
                        return (bindingdataError, defaultExceptionSteps);
                    }

                    // ⚠️ TẤT CẢ exception steps đều được map cụ thể mà không match → Chạy childSteps thông thường
                    var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep != true).ToList();
                    return (bindingdataError, childSteps);
                }
            }
            catch (Exception ex)
            {
                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Thông báo call API Lỗi trên Workflow",
                    MailBody = $"Exception {ex.Message}",
                    WFID = step.WfId,
                    StepID = step.Id,
                    typeWarning = TypeWarning.CallAPIError
                });

                // Exception được xử lý ở LoadStep() - throw để catch block ở đó xử lý
                throw;
            }
        }

        /// <summary>
        /// DummyResponse
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runDumpResponse(WFStep step, object param, string tenantId)
        {
            try
            {
                //*****2023-10-02 start updateDummy*****
                //Update đẩy dạng list Models

                var config = step.Config;

                // replate
                config = replaceProperty(config ?? "", "", param);

                //replate by cache
                List<string> strarrDummy = findTag(config);

                strarrDummy.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        if (cache.Value?.Obj != null)
                        {
                            config = replacePropertyCache(objid, config, "", cache.Value?.Obj ?? new { }, tenantId);
                        }
                    }
                });
                //*****2023-10-02 End updateDummy*****
                //Update đẩy dạng list Models
                var bindingdata03 = this._modelControl.BindingData(step.Response, config, tenantId);
                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingdata03
                    });
                }
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingdata03,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingdata03, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Điều kiện rẽ nhánh
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runCondition(WFStep step, object param, string tenantId)
        {
            try
            {
                var bindingdata04 = this._modelControl.BindingData(step.Response, param, tenantId);
                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingdata04
                    });
                }
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingdata04,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });
                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingdata04, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Thực thi câu lệnh SQL
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="wfName"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runSQLExecute(WFStep step, object param, string wfName, string tenantId)
        {
            DBConfig dbConfig = new DBConfig();
            string keyExecuteRunApiThirdParty = Guid.NewGuid().ToString();
            DatabaseConnection dbConnectionInfo = new DatabaseConnection();
            DateTime CreateDate = DateTime.Now;

            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                dbConfig = JsonConvert.DeserializeObject<DBConfig>(step.Config) ?? new DBConfig();
                if (dbConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                dbConfig.Query = replaceProperty(dbConfig.Query ?? "", "", param);
                List<string> strarr = findTag(dbConfig.Query);

                strarr.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        dbConfig.Query = replacePropertyCache(objid, dbConfig.Query, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                if (string.IsNullOrEmpty(dbConfig.FieldName))
                {
                    throw new IboxLog(string.Format("field name is not set in step {0}", step.Id), tenantId);
                }

                object bindingdata05;
                object dataQuerry;

                if (this.tenantContext == null)
                {
                    var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

                    this.tenantContext = context.GetTenantContext(tenantId).Context;
                }

                dbConnectionInfo = this.tenantContext.Context.DatabaseConnections.FirstOrDefault(ptr => ptr.Id == dbConfig.DatabaseId && !ptr.IsDelete);

                if (dbConnectionInfo == null)
                {
                    throw new IboxLog(string.Format("Database connection ID {0} is not found or deleted", dbConfig.DatabaseId), tenantId);
                }

                List<Dictionary<string, object>> dto = new List<Dictionary<string, object>>();

                if (!dbConfig.ExecuteQuerryAsync)
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = dbConfig.Query,
                        StepID = step.Id,
                        StepName = string.Format("SQL execute querry detail for step - {0}", step.Description),
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });

                    dto = _connection.SetConnection(dbConnectionInfo).OpenConnect().ExecuteQuery(dbConfig.Query ?? "", tenantId);
                    switch (dbConfig.ConvertType)
                    {
                        case ConvertType.Value:
                            var firtRecord = dto.FirstOrDefault();
                            dataQuerry = (firtRecord == null ? "" : firtRecord.FirstOrDefault(ptr => ptr.Key == dbConfig.FieldName.Split('.').LastOrDefault()).Value);
                            break;

                        case ConvertType.Object:
                            var firtRecord1 = dto.FirstOrDefault();
                            dataQuerry = JsonConvert.SerializeObject(firtRecord1);
                            break;

                        case ConvertType.Array:
                            dataQuerry = dto;
                            break;

                        default:
                            dataQuerry = dto;
                            break;
                    }

                    bindingdata05 = _modelControl.BindingDataToProperty(step.Response, dbConfig.FieldName.Split('.').LastOrDefault() ?? "", dataQuerry, tenantId);
                }
                else
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = dbConfig.Query,
                        StepID = step.Id,
                        StepName = string.Format("SQL execute async querry detail for step - {0}", step.Description),
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });
                    _connection.SetConnection(dbConnectionInfo).OpenConnect().ExecuteQueryAsync(dbConfig.Query ?? "", tenantId);
                    bindingdata05 = param;
                }

                _restLog.WriteFileLogSQL(IBGlobalConfig.ServiceName, new ExecuteQuerySQLModel()
                {
                    TenantId = tenantId,
                    WfId = step.WfId,
                    StepName = step.Description,
                    WFName = wfName,
                    StepId = step.Id,
                    SiteRun = IBGlobalConfig.ThisSite,
                    KeyExecuteRunQuerySQL = keyExecuteRunApiThirdParty,
                    Status = ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                    Address = dbConnectionInfo.Address,
                    Catalog = dbConnectionInfo.Catalog,
                    Query = dbConfig.Query ?? "",
                    RecordCount = dto.Count,
                    Reason = null,
                    CreatedDate = CreateDate,
                    ModificationDate = DateTime.Now
                });

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingdata05
                    });
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingdata05,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingdata05, childSteps);
            }
            catch (SqlException ex)
            {
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = "",
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = ex.Message,
                    WorkflowId = step.WfId
                });
                //step.ExceptionCallback();
                return (param, step.ChildSteps);
            }
            catch (Exception ex)
            {
                _restLog.WriteFileLogSQL(IBGlobalConfig.ServiceName, new ExecuteQuerySQLModel()
                {
                    TenantId = tenantId,
                    WfId = step.WfId,
                    StepName = step.Description,
                    WFName = wfName,
                    StepId = step.Id,
                    SiteRun = IBGlobalConfig.ThisSite,
                    KeyExecuteRunQuerySQL = keyExecuteRunApiThirdParty,
                    Status = ExecuteRunApiThirdPartyStatus.Fail.ToString(),
                    Address = dbConnectionInfo?.Address ?? "",
                    Catalog = dbConnectionInfo?.Catalog ?? "",
                    Query = dbConfig.Query ?? "",
                    RecordCount = null,
                    Reason = ex.Message,
                    CreatedDate = CreateDate,
                    ModificationDate = DateTime.Now
                });
                //step.ExceptionCallback();
                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Error Step RunSQLExecute",
                    MailBody = $"Exception: {ex.Message}",
                    WFID = step.WfId,
                    StepID = step.Id,
                    typeWarning = TypeWarning.ErrorRunSQL
                });
                throw;
            }
        }

        /// <summary>
        /// delay
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runDelay(WFStep step, object param, string tenantId)
        {
            try
            {
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = param,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                DelayConfig delayConfig = JsonConvert.DeserializeObject<DelayConfig>(step.Config) ?? throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = param
                    });
                }

                Thread.Sleep(1000 * (delayConfig.Time ?? 0));
                return (param, step.ChildSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// recall workflow
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="wfName"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runRecallWF(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                RecallWFConfig recallWFConfig = JsonConvert.DeserializeObject<RecallWFConfig>(step.Config) ?? new RecallWFConfig();
                if (recallWFConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                if (!_workflowControl.IsExist(recallWFConfig.Wfid ?? "", tenantId))
                {
                    _workflowControl.Deploy(recallWFConfig.Wfid ?? "", tenantId);
                }
                var wf = _workflowControl.GetWFDeployByID(recallWFConfig.Wfid ?? "", tenantId);

                recallWFConfig.BodyRequest = replaceProperty(recallWFConfig.BodyRequest ?? "", "", param);
                List<string> strarr1 = findTag(recallWFConfig.BodyRequest);

                strarr1.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        recallWFConfig.BodyRequest = replacePropertyCache(objid, recallWFConfig.BodyRequest, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                dynamic responseBackStep = LoadStep(wf.WFstep, recallWFConfig.BodyRequest, wfName, tenantId);

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = responseBackStep
                    });
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = responseBackStep,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (responseBackStep, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Duyệt mảng
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="wfName"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runReadArray(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                ReadArraysConfig readArrayConfig = JsonConvert.DeserializeObject<ReadArraysConfig>(step.Config) ?? throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);

                readArray(readArrayConfig, step, step.Param ?? string.Empty, param, wfName, tenantId);

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (param, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Gửi mail
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runSendEmail(WFStep step, object param, string tenantId)
        {
            try
            {
                //*****05/10/2023 Thêm bước thực hiện gửi mail ******
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                MailConfig mailConfig = JsonConvert.DeserializeObject<MailConfig>(step.Config) ?? new MailConfig();
                if (mailConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = param
                    });
                }

                if (this.tenantContext == null)
                {
                    var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

                    this.tenantContext = context.GetTenantContext(tenantId).Context;
                }

                MailServerInfo? mailServerInfo = this.tenantContext.Context.MailServerConnections.Select(ptr => new MailServerInfo()
                {
                    Id = ptr.Id,
                    Smtp_Host = ptr.Smtp_Host,
                    Smtp_Port = ptr.Smtp_Port,
                    Smtp_Username = ptr.Smtp_Username,
                    Smtp_Password = ptr.Smtp_Password,
                    Smtp_Credentials = ptr.Smtp_Credentials,
                    Smtp_EnableSSL = ptr.Smtp_EnableSSL,
                }).FirstOrDefault(ptr => ptr.Id == mailConfig.MailServerID);

                if (mailServerInfo == null)
                {
                    throw new IboxLog(string.Format("Can not found mail server info in step {0}", step.Id), tenantId);
                }

                mailConfig.MailBody = replaceProperty(mailConfig.MailBody ?? "", "", param, true);

                //replate by cache
                List<string> strarrDummy1 = findTag(mailConfig.MailBody);

                strarrDummy1.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        mailConfig.MailBody = replacePropertyCache(objid, mailConfig.MailBody, "", cache.Value?.Obj ?? new { }, tenantId, true);
                    }
                });

                mailConfig.MailTitle = replaceProperty(mailConfig.MailTitle ?? "", "", param);

                //replate by cache
                List<string> strarrDummyTitle = findTag(mailConfig.MailTitle);

                strarrDummyTitle.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        mailConfig.MailTitle = replacePropertyCache(objid, mailConfig.MailTitle, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = new
                    {
                        mailConfig = mailConfig,
                        mailServerInfo = new MailServerInfo
                        {
                            Id = mailServerInfo.Id,
                            Smtp_Host = mailServerInfo.Smtp_Host,
                            Smtp_Port = mailServerInfo.Smtp_Port,
                            Smtp_Username = mailServerInfo.Smtp_Username,
                            Smtp_Credentials = mailServerInfo.Smtp_Credentials,
                            Smtp_EnableSSL = mailServerInfo.Smtp_EnableSSL,
                        }
                    },
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });
                _smtp.Send(mailServerInfo, mailConfig);

                //*****05/10/2023 end ******

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (param, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Build list object
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runBuildListObject(WFStep step, object param, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                BuildListConfig buildListConfig = JsonConvert.DeserializeObject<BuildListConfig>(step.Config) ?? new BuildListConfig();
                if (buildListConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                buildListConfig.Item = replaceProperty(buildListConfig.Item ?? "", "", param);
                List<string> strarr2 = findTag(buildListConfig.Item);

                strarr2.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        buildListConfig.Item = replacePropertyCache(objid, buildListConfig.Item, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });
                if (string.IsNullOrEmpty(buildListConfig.FieldName))
                {
                    throw new IboxLog(string.Format("Can not found FieldName config in step {0}", step.Id), tenantId);
                }

                var data = getCache(step.Id ?? "");

                if (string.IsNullOrEmpty(step.Response))
                {
                    throw new IboxLog(string.Format("Can not found response model config in step {0}", step.Id), tenantId);
                }

                Type type = _modelControl.GetType(step.Response, tenantId);
                object bindingdata06;
                if (data == null)
                {
                    var response = _modelControl.BindingDataDump(step.Response, tenantId);
                    bindingdata06 = _modelControl.BindingDataToProperty(type, JsonConvert.DeserializeObject(response.ToString(), type), buildListConfig?.FieldName?.Split('.').LastOrDefault() ?? "", buildListConfig.Item, false, tenantId);
                }
                else
                {
                    bindingdata06 = _modelControl.BindingDataToProperty(type, data.Obj, buildListConfig?.FieldName?.Split('.').LastOrDefault() ?? "", buildListConfig.Item, false, tenantId);
                }

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = bindingdata06
                });

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingdata06,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingdata06, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Formatting string and list data
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <param name="wfName"></param>
        /// <param name="tenantId"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private dynamic runFormating(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                FormattingConfig formattingConfig = GetConfig<FormattingConfig>(step);

                switch (formattingConfig.FormatType)
                {
                    case FormatType.AddEnd:
                        return AddEnd(step, param, wfName, tenantId);

                    case FormatType.Replate:
                        return Replate(step, param, wfName, tenantId);

                    case FormatType.SplitToObject:
                        return SplitToObject(step, param, wfName, tenantId);

                    case FormatType.ListDataToListObject:
                        return ListDataToListObject(step, param, wfName, tenantId);

                    case FormatType.GetGUID:
                        return AutoGenerateData(step, param, AutoGenerateDataType.GUID, wfName, tenantId);

                    case FormatType.RandomNumber:
                        return AutoGenerateData(step, param, AutoGenerateDataType.Number, wfName, tenantId);

                    case FormatType.GetDateTime:
                        return AutoGenerateData(step, param, AutoGenerateDataType.Datetime, wfName, tenantId);

                    default:
                        return this._modelControl.BindingData(step.Response, param, tenantId);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Tìm kiếm trong mảng
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runFindInList(WFStep step, object param, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                var config = JsonConvert.DeserializeObject<FindConfig>(step.Config) ?? new FindConfig();

                if (string.IsNullOrEmpty(config.FieldName) || string.IsNullOrEmpty(config.AddToField))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                var value = _modelControl.GetValue(param, config.FieldName);

                string addToFieldName = config.AddToField.Split(".").LastOrDefault() ?? "";

                if (value == null)
                {
                    return (new { }, step.ChildSteps);
                }

                switch (config.FindOption)
                {
                    case FindOption.Firt:
                        foreach (var item in (IList)value)
                        {
                            var itemvalue = validDataForFindInList(item, config);
                            if (itemvalue != null)
                            {
                                return (_modelControl.BindingDataToProperty(step.Response, addToFieldName, JsonConvert.SerializeObject(item), tenantId), step.ChildSteps);
                            }
                        }
                        return (new { }, step.ChildSteps);

                    case FindOption.Last:
                        for (int index = (((IList)value).Count - 1); index >= 0; --index)
                        {
                            var item = ((IList)value)[index] ?? new { };
                            var itemvalue = validDataForFindInList(item, config);
                            if (itemvalue != null)
                            {
                                return (_modelControl.BindingDataToProperty(step.Response, addToFieldName, JsonConvert.SerializeObject(item), tenantId), step.ChildSteps);
                            }
                        }
                        return (new { }, step.ChildSteps);

                    case FindOption.Any:
                    default:
                        var type = value.GetType();

                        var listX = (IList)Activator.CreateInstance(type);

                        foreach (var item in (IList)value)
                        {
                            var itemvalue = validDataForFindInList(item, config);
                            if (itemvalue != null)
                            {
                                listX.Add(item);
                            }
                        }
                        return (_modelControl.BindingDataToProperty(step.Response, addToFieldName, listX, tenantId), step.ChildSteps);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Thực thi câu lệnh database informix
        /// </summary>
        /// <param name="step"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private (dynamic, List<WFStep>) runInformixExecute(WFStep step, object param, string tenantId)
        {
            DBConfig dbConfig = new DBConfig();
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                dbConfig = JsonConvert.DeserializeObject<DBConfig>(step.Config) ?? new DBConfig();
                if (dbConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                dbConfig.Query = replaceProperty(dbConfig.Query ?? "", "", param);
                List<string> strarr = findTag(dbConfig.Query);

                strarr.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        dbConfig.Query = replacePropertyCache(objid, dbConfig.Query, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                if (string.IsNullOrEmpty(dbConfig.FieldName))
                {
                    throw new IboxLog(string.Format("field name is not set in step {0}", step.Id), tenantId);
                }

                object bindingData;
                object dataQuery;

                var dbConnectionInfo = this.tenantContext.Context.DatabaseConnections.FirstOrDefault(ptr => ptr.Id == dbConfig.DatabaseId && !ptr.IsDelete);

                if (dbConnectionInfo == null)
                {
                    throw new IboxLog(string.Format("DatabaseInformix connection ID {0} is not found or deleted", dbConfig.DatabaseId), tenantId);
                }

                if (dbConnectionInfo.Address == null)
                {
                    throw new IboxLog(string.Format("DatabaseInformix connection ID {0} is not found or deleted", dbConfig.DatabaseId), tenantId);
                }

                if (!dbConfig.ExecuteQuerryAsync)
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = dbConfig.Query,
                        StepID = step.Id,
                        StepName = string.Format("Informix execute querry detail for step - {0}", step.Description),
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });

                    var dto = _connection.SetConnection("").ExecuteQueryInformIx(dbConnectionInfo.Address, dbConfig.Query, tenantId);
                    var firtRecord = dto.FirstOrDefault();

                    switch (dbConfig.ConvertType)
                    {
                        case ConvertType.Value:
                            dataQuery = (firtRecord == null ? "" : firtRecord.FirstOrDefault(ptr => ptr.Key == dbConfig.FieldName.Split('.').LastOrDefault()).Value);
                            break;

                        case ConvertType.Object:
                            dataQuery = JsonConvert.SerializeObject(firtRecord);
                            break;

                        case ConvertType.Array:
                        default:
                            dataQuery = dto;
                            break;
                    }

                    bindingData = _modelControl.BindingDataToProperty(step.Response, dbConfig.FieldName.Split('.').LastOrDefault() ?? "", dataQuery, tenantId);
                }
                else
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = dbConfig.Query,
                        StepID = step.Id,
                        StepName = string.Format("Informix execute async querry detail for step - {0}", step.Description),
                        ErrorMessage = string.Empty,
                        WorkflowId = step.WfId
                    });

                    _connection.SetConnection("").ExecuteQueryInformIxAsync(dbConnectionInfo.Address, dbConfig.Query, tenantId);

                    bindingData = param;
                }

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingData
                    });
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = bindingData,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var childSteps = step.ChildSteps.Where(ptr => ptr.IsExceptionStep == false || ptr.IsExceptionStep == null).ToList();

                return (bindingData, childSteps);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private (dynamic, List<WFStep>) runHttpStatusResponse(WFStep step, object param, string tenantId, ref HttpResponse? httpResponse)
        {
            HttpStatusResponseConfig httpStatusResponseConfig = new HttpStatusResponseConfig();
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                httpStatusResponseConfig = JsonConvert.DeserializeObject<HttpStatusResponseConfig>(step.Config);

                if (httpStatusResponseConfig == null)
                {
                    throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), tenantId);
                }

                if (httpResponse != null)
                {
                    httpResponse.StatusCode = httpStatusResponseConfig.Code;
                }

                return (param, step.ChildSteps);
            }
            catch (Exception ex)
            {
                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Error Step RunInformixExecute",
                    MailBody = $"Exception: {ex.Message}",
                    WFID = step.WfId,
                    StepID = step.Id,
                    typeWarning = TypeWarning.ErrorRunInformix,
                });
                throw;
            }
        }

        /// <summary>
        /// Push message tới Kafka
        /// </summary>
        private (dynamic, List<WFStep>) runKafkaPush(WFStep step, object param, string tenantId)
        {
            Log.Information("method runKafkaPush is running");
            try
            {
                if (string.IsNullOrEmpty(step.Config))
                {
                    throw new IboxLog($"Can not found Config in step {step.Id}", tenantId);
                }

                var config = JsonConvert.DeserializeObject<dynamic>(step.Config);
                if (config == null)
                {
                    throw new IboxLog($"Can not parse Config in step {step.Id}", tenantId);
                }

                string bootstrapServers = config.bootstrapServers?.ToString();
                string topic = config.topic?.ToString();
                string messageBody = config.messageBody ?? "test message";
                string partitionKey = config.partitionKey?.ToString() ?? "";
                string messageFormat = config.messageFormat?.ToString() ?? "JSON";

                if (string.IsNullOrWhiteSpace(bootstrapServers) || string.IsNullOrWhiteSpace(topic))
                {
                    throw new IboxLog($"Kafka step {step.Id} requires bootstrapServers and topic.", tenantId);
                }

                // Replace placeholders trong message body
                messageBody = replaceProperty(messageBody, "", param);
                var tags = findTag(messageBody);
                foreach (var t in tags)
                {
                    if (string.IsNullOrEmpty(t)) continue;
                    var objid = t.Split('.')[0];
                    var cacheEntry = this.caches.FirstOrDefault(c => c.Key == objid);
                    messageBody = replacePropertyCache(objid, messageBody, "", cacheEntry.Value?.Obj ?? new { }, tenantId);
                }
                var bindingdata04 = this._modelControl.BindingData(step.Response, config.messageBody, tenantId);
                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = bindingdata04
                    });
                }
                Log.Information("bindingdata04: {BindingData}", JsonConvert.SerializeObject(bindingdata04));
                if (string.IsNullOrEmpty(messageBody))
                {
                    messageBody = JsonConvert.SerializeObject(param);
                }

                Log.Information("[KafkaPush - Step {StepId}] Transformed Message Body: {MessageBody}", step.Id, messageBody);

                // Gửi message qua IKafkaProducer (đã inject trong constructor ExecuteWF)
                // Xử lý headers nếu có
                Dictionary<string, string> headersDict = null;
                if (config.headers != null)
                {
                    headersDict = new Dictionary<string, string>();
                    foreach (var header in config.headers)
                    {
                        string key = header.key?.ToString();
                        string value = header.value?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            headersDict[key] = value ?? "";
                        }
                    }
                }

                string partitionKeyValue = !string.IsNullOrEmpty(partitionKey) ? partitionKey : null;
                Log.Information("[KafkaPush - Step {StepId}] Sending to Topic: {Topic} at {Broker} | PartitionKey: {PartitionKey} | HasHeaders: {HasHeaders}", step.Id, topic, bootstrapServers, partitionKeyValue, headersDict != null && headersDict.Count > 0);
                if (headersDict != null && headersDict.Count > 0)
                {
                    _kafkaProducer.ProduceMessageWithHeadersAsync(bootstrapServers, topic, messageBody, headersDict, partitionKeyValue).GetAwaiter().GetResult();
                }
                else
                {
                    _kafkaProducer.ProduceMessageAsync(bootstrapServers, topic, messageBody, partitionKeyValue).GetAwaiter().GetResult();
                }

                var responseBody = ParseJsonOrKeepString(messageBody);

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = responseBody,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                if (step.SaveResponseToCache ?? false)
                {
                    saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                    {
                        ObjType = this._modelControl.GetType(step.Response, tenantId),
                        Obj = param
                    });
                }

                var childSteps = step.ChildSteps?.Where(ptr => ptr.IsExceptionStep != true).ToList() ?? new List<WFStep>();
                return (param, childSteps);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[KafkaPush - Step {StepId}] FATAL ERROR when pushing to topic Param: {Param}", step.Id, JsonConvert.SerializeObject(param));
                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = "",
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = ex.Message,
                    WorkflowId = step.WfId
                });
                throw;
            }
        }
    
    }
}