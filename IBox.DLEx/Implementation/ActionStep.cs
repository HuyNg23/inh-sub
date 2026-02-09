using DocumentFormat.OpenXml;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using IBox.DLEx.Model;
using IBox.Workflow.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Context;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using ReadArraysConfig = IBox.DLEx.Model.ReadArraysConfig;
using RunType = IBox.DLEx.Model.RunType;

namespace IBox.DLEx.Implementation
{
    public partial class ExecuteWF
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        #region Xử lý nghiệp vụ cho từng loại thực hiện

        private List<WFStep> condition(string config, WFStep currentStep, List<WFStep>? wFstep, object obj, string typeid, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(config))
                {
                    throw new IboxLog(string.Format("config is null or empty in step {0}", currentStep.Id), tenantId);
                }

                var configCondition = JsonConvert.DeserializeObject<Condition>(config);
                if (configCondition == null)
                {
                    throw new IboxLog(string.Format("config is null in step {0}", currentStep.Id), tenantId);
                }

                if (wFstep == null)
                {
                    throw new IboxLog(string.Format("Child step was not found in step: {0}", currentStep.Id), tenantId);
                }

                if (loadCondition(currentStep, configCondition.ConditionItems ?? new List<ConditionItem>(), obj, typeid, tenantId))
                {
                    return wFstep.Where(ptr => ptr.Id == configCondition.NextStepTrue).ToList();
                }

                return wFstep.Where(ptr => ptr.Id == configCondition.NextStepFalse).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void readArray(ReadArraysConfig readArrayConfig, WFStep currentStep, string typeid, object param, string wfName, string tenantId)
        {
            try
            {
                var WorkflowIDExec = _workflowControl.GetWFDeployByID(readArrayConfig.Wfid ?? "", tenantId);
                if (WorkflowIDExec == null)
                {
                    Deploy(readArrayConfig.Wfid ?? "", tenantId);
                    WorkflowIDExec = _workflowControl.GetWFDeployByID(readArrayConfig.Wfid ?? "", tenantId);
                }

                if (string.IsNullOrEmpty(typeid))
                {
                    throw new IboxLog(string.Format("Type Param in step {0} is empty", currentStep.Id), tenantId);
                }

                Type model = this._modelControl.GetType(typeid, tenantId);
                if (model == null)
                {
                    throw new IboxLog(string.Format("Type is null in step {0}", currentStep.Id), tenantId);
                }

                PropertyAndValue propertyAndValue = new PropertyAndValue().GetPropertyInfoAndValue(new PropertyAndValue()
                {
                    FieldName = readArrayConfig.FieldName,
                    Type = model,
                    Obj = param
                }, 0) ?? new PropertyAndValue();

                List<object> list = new List<object>();

                if (propertyAndValue.PropertyInfo == null)
                {
                    list = new List<object>();
                }
                else
                {
                    if (propertyAndValue.PropertyInfo.PropertyType.Name == typeof(List<>).Name)
                    {
                        list = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(propertyAndValue.Value)) ?? new List<object>();
                    }
                }

                switch (readArrayConfig.RunType)
                {
                    case RunType.asyncs:
                        int i = 0;
                        foreach (var ptr in list)
                        {
                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = _modelControl.BindingData(currentStep.Param, ptr.ToString(), tenantId),
                                ResponseBody = "",
                                StepID = currentStep.Id,
                                StepName = currentStep.Description,
                                ErrorMessage = i.ToString(),
                                WorkflowId = currentStep.WfId
                            });
                            i = i + 1;
                            LoadStep(WorkflowIDExec.WFstep, ptr, wfName, tenantId);
                        };
                        break;

                    case RunType.sync:
                        int x = 0;
                        foreach (var ptr in list)
                        {
                            object response = LoadStep(WorkflowIDExec.WFstep, ptr, wfName, tenantId);
                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = _modelControl.BindingData(currentStep.Param, ptr.ToString(), tenantId),
                                ResponseBody = response,
                                StepID = currentStep.Id,
                                StepName = currentStep.Description,
                                ErrorMessage = x.ToString(),
                                WorkflowId = currentStep.WfId
                            });
                            x = x + 1;
                        }
                        break;

                    default:
                        int d = 0;
                        foreach (var ptr in list)
                        {
                            object response = LoadStep(WorkflowIDExec.WFstep, ptr, wfName, tenantId);
                            var workstep = WorkflowIDExec.WFstep.FirstOrDefault();
                            if (workstep == null)
                            {
                                throw new IboxLog($"work step was not found or work step haven't any", tenantId);
                            }

                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = _modelControl.BindingData(workstep.Param, ptr.ToString(), tenantId),
                                ResponseBody = response,
                                StepID = currentStep.Id,
                                StepName = currentStep.Description,
                                ErrorMessage = d.ToString(),
                                WorkflowId = currentStep.WfId
                            });
                            d = d + 1;
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        private bool loadCondition(WFStep currentStep, List<ConditionItem> conditionItems, object obj, string typeid, string tenantId)
        {
            try
            {
                if (conditionItems == null || conditionItems.Count == 0)
                {
                    return true;
                }

                if (string.IsNullOrEmpty(typeid))
                {
                    throw new IboxLog(string.Format("Type Param in step {0} is empty", currentStep.Id), tenantId);
                }

                Type model = this._modelControl.GetType(typeid, tenantId);
                if (model == null)
                {
                    throw new IboxLog(string.Format("Type is null in step {0}", currentStep.Id), tenantId);
                }

                foreach (ConditionItem citem in conditionItems)
                {
                    if (citem == null)
                    {
                        throw new IboxLog(string.Format("Condition is null in config for step {0}", currentStep.Id), tenantId);
                    }

                    if (string.IsNullOrEmpty(citem.FieldName))
                    {
                        throw new IboxLog(string.Format("FieldName is null in config for step {0}", currentStep.Id), tenantId);
                    }

                    PropertyAndValue? property = new PropertyAndValue().GetPropertyInfoAndValue(new PropertyAndValue()
                    {
                        Type = model,
                        FieldName = citem.FieldName,
                        Obj = obj
                    }, 0);

                    if (property == null || property.PropertyInfo == null)
                    {
                        throw new IboxLog(string.Format("step {0} not contain property {1}", currentStep.Id, citem.FieldName), tenantId);
                    }

                    if (citem.ConditionType == null)
                    {
                        throw new IboxLog(string.Format("ConditionType is not found in step {0}", currentStep.Id), tenantId);
                    }
                    if (!string.IsNullOrEmpty(citem.Value))
                    {
                        citem.Value = replaceProperty(citem.Value, "", obj);
                        List<string> strarr = findTag(citem.Value);

                        strarr.ForEach(ptr =>
                        {
                            if (!string.IsNullOrEmpty(ptr))
                            {
                                var objid = ptr.Split('.')[0];
                                var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                                citem.Value = replacePropertyCache(objid, citem.Value, "", cache.Value?.Obj ?? new { }, tenantId);
                            }
                        });
                    }
                    switch (citem.ConditionType)
                    {
                        #region Number

                        case ConditionType.NumberLess:
                            if (decimal.Parse(property.Value?.ToString() ?? "0") < decimal.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.NumberLessOrEqual:
                            if (decimal.Parse(property.Value?.ToString() ?? "0") <= decimal.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.NumberBigger:
                            if (decimal.Parse(property.Value?.ToString() ?? "0") > decimal.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.NumberBiggerOrEqual:
                            if (decimal.Parse(property.Value?.ToString() ?? "0") >= decimal.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.NumberEqual:
                            if (decimal.Parse(property.Value?.ToString() ?? "0") == decimal.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        #endregion Number

                        #region Datetime

                        case ConditionType.DateLess:
                            if (DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US")) < DateTime.Parse(citem.Value ?? "", new CultureInfo("en-US")))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.DateLessOrEqual:
                            if (DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US")) <= DateTime.Parse(citem.Value ?? "", new CultureInfo("en-US")))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.DateBigger:
                            if (DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US")) > DateTime.Parse(citem.Value ?? "", new CultureInfo("en-US")))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.DateBiggerOrEqual:
                            if (DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US")) >= DateTime.Parse(citem.Value ?? "", new CultureInfo("en-US")))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.DateEqual:
                            if (DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US")) == DateTime.Parse(citem.Value ?? "", new CultureInfo("en-US")))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.DateSubtractionNowBiggerOrEqual:
                            DateTime dt = DateTime.Parse(property.Value?.ToString() ?? "", new CultureInfo("en-US"));
                            if (DateTime.Now.Subtract(dt).TotalDays >= double.Parse(citem.Value ?? "0"))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        #endregion Datetime

                        #region Text

                        case ConditionType.TextEqual:
                            if (property.Value?.ToString()?.Equals(citem.Value) == true)
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.TextContain:
                            if (property.Value?.ToString()?.Contains(citem.Value ?? "----------") == true)
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        case ConditionType.TextIsNullOrEmpty:
                            if (string.IsNullOrEmpty(property.Value?.ToString()))
                            {
                                break;
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        #endregion Text

                        #region object

                        case ConditionType.ObjectIsNull:
                            if (property.Value == null)
                            {
                                break;
                            }

                            if (property.GetType().GenericTypeArguments.Count() <= 0)
                            {
                                if (property.Value is IList)
                                {
                                    IList list = (IList)property.Value;
                                    if (list.Count == 0)
                                    {
                                        break;
                                    }
                                }
                            }

                            if (citem.Conditions?.Count > 0)
                            {
                                return loadCondition(currentStep, citem.Conditions, obj, typeid, tenantId);
                            }
                            return false;

                        #endregion object

                        default:
                            break;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic runDLL(WFStep wFStep, string dllID, object param, string function = "", string tenantId = "AppLogs")
        {
            try
            {
                if (string.IsNullOrEmpty(dllID))
                {
                    throw new IboxLog("dllID is null, review work step config or log for more detail", tenantId);
                }
                var dllInfo = wFStep.DynamicLinkedLibrary;
                if (dllInfo == null)
                {
                    throw new IboxLog("can not found dll", tenantId);
                }

                if (string.IsNullOrEmpty(dllInfo.Link))
                {
                    throw new IboxLog("can not found dll in local server", tenantId);
                }

                if (string.IsNullOrEmpty(dllInfo.Config))
                {
                    throw new IboxLog("can not found dll config", tenantId);
                }

                string fullPath = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + dllInfo.Link;
                string[] fileName = dllInfo.Link.Split(new char[] { '/' });

                if (!File.Exists(fullPath))
                {
                    bool isDownloaded = DownloadDllIfExistsAsync(fileName[1]).Result;

                    if (!isDownloaded || !File.Exists(fullPath))
                    {
                        throw new IboxLog($"DLL {fileName[1]} not exist.", tenantId);
                    }
                }

                var dll = Assembly.LoadFile(fullPath);

                var response = new object();

                foreach (Type type in dll.GetExportedTypes().Where(ptr => !ptr.IsAbstract && !ptr.IsInterface && ptr.GetMethods().Any(x => x.Name == function)).ToList())
                {
                    object? instance = null;
                    var constructor = type.GetConstructors()
                      .FirstOrDefault(ctor => ctor.GetParameters().Length == 2 &&
                               ctor.GetParameters()[0].ParameterType == typeof(IConfiguration) &&
                               ctor.GetParameters()[1].ParameterType == typeof(IEncryption));

                    if (constructor != null)
                    {
                        instance = Activator.CreateInstance(type, _configuration, _encryption);
                        if (instance == null)
                        {
                            throw new IboxLog("Can not load Dll. Maybe DLL is not exist or wrong format!", tenantId);
                        }
                    }
                    else
                    {
                        instance = Activator.CreateInstance(type);
                        if (instance == null)
                        {
                            throw new IboxLog("Can not load Dll. Maybe DLL is not exist or wrong format!", tenantId);
                        }
                    }

                    var methods = instance.GetType().GetMethods();
                    if (methods.Any(ptr => ptr.Name == function))
                    {
                        response = type.InvokeMember(function,
                            BindingFlags.InvokeMethod,
                            null,
                            instance,
                            new object[] { dllInfo.Config, param, caches });
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Error(ex.InnerException, ex.InnerException?.Message ?? "");
                throw new IboxLog(ex.InnerException?.Message ?? ex.Message, tenantId, ex.InnerException);
            }
        }

        /// <summary>
        /// Kiểm tra xem file DLL có tồn tại trên các server hay không, nếu có thì tải file về.
        /// </summary>
        /// <param name="fileName">Tên file DLL cần tải.</param>
        /// <returns>Trả về `true` nếu tải thành công, ngược lại trả về `false`.</returns>
        private async Task<bool> DownloadDllIfExistsAsync(string fileName)
        {
            var services = IBGlobalConfig.ServersIBox
                .Concat(IBGlobalConfig.RootServersIBox)
                .Concat(IBGlobalConfig.RestServiceIBox)
                .Concat(IBGlobalConfig.ScheduleServiceIBox)
                .Concat(IBGlobalConfig.LogServiceIBox)
                .Concat(IBGlobalConfig.ChatBotServiceIBox)
                .ToList();

            foreach (var ip in services)
            {
                try
                {
                    if (await IsFileAvailableAsync(ip, fileName))
                    {
                        return await DownloadFileAsync(ip, fileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to sync {fileName} from {ip}. Error: {ex}");
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem file có tồn tại trên một server cụ thể không.
        /// </summary>
        /// <param name="ip">Địa chỉ IP hoặc domain của server.</param>
        /// <param name="fileName">Tên file cần kiểm tra.</param>
        /// <returns>Trả về `true` nếu file tồn tại, ngược lại trả về `false`.</returns>
        private async Task<bool> IsFileAvailableAsync(string ip, string fileName)
        {
            string checkUrl = $"{ip}/api/DLL/CheckFileExist?fileName={fileName}";
            HttpResponseMessage response = await _httpClient.GetAsync(checkUrl);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            };

            string responseBody = await response.Content.ReadAsStringAsync();
            return bool.TryParse(responseBody, out bool fileExists) && fileExists;
        }

        /// <summary>
        /// Tải file từ một server và lưu vào thư mục cục bộ.
        /// </summary>
        /// <param name="ip">Địa chỉ IP hoặc domain của server.</param>
        /// <param name="fileName">Tên file cần tải.</param>
        /// <returns>Trả về `true` nếu tải thành công, ngược lại trả về `false`.</returns>
        private async Task<bool> DownloadFileAsync(string ip, string fileName)
        {
            string downloadUrl = $"{ip}/api/DLL/DownloadFile?fileName={fileName}";
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "DLL", fileName);

            HttpResponseMessage fileResponse = await _httpClient.GetAsync(downloadUrl);
            if (!fileResponse.IsSuccessStatusCode)
            {
                return false;
            };

            await using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fileResponse.Content.CopyToAsync(fileStream);

            Log.Information($"Successfully downloaded {fileName} from {ip}");
            return true;
        }

        private async Task<bool> SyncFileDllNotExistsAsync(string fileName, string tenantId = "AppLogs")
        {
            List<string> services = new List<string>();
            services.AddRange(IBGlobalConfig.ServersIBox);
            services.AddRange(IBGlobalConfig.RootServersIBox);
            services.AddRange(IBGlobalConfig.RestServiceIBox);
            services.AddRange(IBGlobalConfig.ScheduleServiceIBox);
            services.AddRange(IBGlobalConfig.LogServiceIBox);
            services.AddRange(IBGlobalConfig.ChatBotServiceIBox);

            using (HttpClient httpClient = new HttpClient())
            {
                foreach (var ip in services)
                {
                    try
                    {
                        string checkUrl = $"{ip}/api/DLL/CheckFileExist?fileName={fileName}";
                        string downloadUrl = $"{ip}/api/DLL/DownloadFile?fileName={fileName}";

                        // Gửi yêu cầu GET để kiểm tra file có tồn tại không
                        HttpResponseMessage response = await httpClient.GetAsync(checkUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            if (bool.TryParse(responseBody, out bool fileExists) && fileExists)
                            {
                                using (LogContext.PushProperty("TenantId", tenantId))
                                {
                                    Log.Information($"Found DLL {fileName} on {ip}, downloading...");
                                }

                                string savePath = Path.Combine(Directory.GetCurrentDirectory(), "DLL", fileName);

                                // Tải file về
                                using (HttpResponseMessage fileResponse = await httpClient.GetAsync(downloadUrl))
                                {
                                    if (fileResponse.IsSuccessStatusCode)
                                    {
                                        using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                                        {
                                            await fileResponse.Content.CopyToAsync(fileStream);
                                        }

                                        using (LogContext.PushProperty("TenantId", tenantId))
                                        {
                                            Log.Information($"Successfully downloaded {fileName} from {ip}");
                                        }

                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to sync {fileName} from {ip}. Error: {ex.Message}");
                    }
                }
            }
            return false;
        }

        private RestAPIResponse callRestAPI(string config, object param, WFStep step, string wfName, string tenantId)
        {
            if (string.IsNullOrEmpty(config))
            {
                throw new IboxLog("config is null or empty", tenantId);
            }

            var configRestAPI = JsonConvert.DeserializeObject<RestAPIRequestWFConfig>(config);
            if (configRestAPI == null)
            {
                throw new IboxLog("config is null", tenantId);
            }

            if (!string.IsNullOrEmpty(configRestAPI.Body))
            {
                // none cache
                configRestAPI.Body = replaceProperty(configRestAPI.Body, "", param);

                // Tìm kiếm các thẻ cần thay thế
                List<string> strarr = findTag(configRestAPI.Body);

                strarr.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        if (cache.Value != null)
                        {
                            configRestAPI.Body = replacePropertyCache(objid, configRestAPI.Body, "", cache.Value?.Obj, tenantId);
                        }
                    }
                });
            }

            //add header
            if (configRestAPI.Headers != null && configRestAPI.Headers.Count > 0)
            {
                foreach (var itemHeader in configRestAPI.Headers.Where(ptr => ptr.Value != null && ptr.Value.Contains('[')).ToList())
                {
                    // none cache
                    itemHeader.Value = replaceProperty(itemHeader.Value, "", param);

                    // Tìm kiếm các thẻ cần thay thế
                    List<string> strarr = findTag(itemHeader.Value);
                    strarr.ForEach(ptr =>
                    {
                        if (!string.IsNullOrEmpty(ptr))
                        {
                            var objid = ptr.Split('.').FirstOrDefault();
                            var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                            if (cache.Value != null)
                            {
                                itemHeader.Value = replacePropertyCache(objid, itemHeader.Value, "", cache.Value?.Obj, tenantId);
                            }
                        }
                    });
                }
            }

            if (configRestAPI.Authen != null && configRestAPI.Authen.Type == AuthorType.Basic)
            {
                string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                               .GetBytes(configRestAPI.Authen.UserName + ":" + configRestAPI.Authen.Password));
                configRestAPI.Headers.Add(new RestAPIHeader()
                {
                    Label = "Authorization",
                    Value = string.Format("Basic {0}", encoded)
                });
            }
            if (configRestAPI.Url != null && configRestAPI.Url.Contains('['))
            {
                var listParamUrl = configRestAPI.Url.Split('[').ToList();
                configRestAPI.Url = replaceProperty(configRestAPI.Url, "", param);

                // using cache
                List<string> strarr = findTag(configRestAPI.Url);
                strarr.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.').FirstOrDefault();
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        if (cache.Value != null)
                        {
                            configRestAPI.Url = replacePropertyCache(objid, configRestAPI.Url, "", cache.Value?.Obj, tenantId);
                        }
                    }
                });
            }

            saveDebug(new ModelXWorkflowDebug
            {
                RequestBody = param,
                ResponseBody = new
                {
                    configRestAPI.Headers,
                    configRestAPI.Url,
                    configRestAPI.Body,
                    configRestAPI.Method
                },
                StepID = "Detail step - " + step.Id,
                StepName = "Detail step - " + step.Description,
                ErrorMessage = string.Empty,
                WorkflowId = step.WfId,
            });

            //Call API
            object body = new object();
            string keyExecuteRunApiThirdParty = Guid.NewGuid().ToString();
            DateTime createDate = DateTime.Now;

            var response2 = new RestAPIResponse();
            List<RestAPIHeader>? headers = new List<RestAPIHeader>();
            RestAPIHeader restAPI = new RestAPIHeader
            {
                Label = "Content-Type",
                Value = "application/json"
            };
            headers.Add(restAPI);
            try
            {
                if (configRestAPI.ExecuteAsync)
                {
                    _workflowControl.DeployIfNotExist(configRestAPI.wfExecuteSuccess, tenantId);
                    _workflowControl.DeployIfNotExist(configRestAPI.wfExecuteError, tenantId);
                    this._restAPI.SendAsync(new RestAPIRequestWFConfig
                    {
                        Url = configRestAPI.Url,
                        Body = configRestAPI.Body,
                        Method = configRestAPI.Method,
                        Headers = configRestAPI.Headers,
                    }, tenantId,
                    async (wfid, responseMessage) =>
                    {
                        if (!string.IsNullOrEmpty(configRestAPI.wfExecuteSuccess))
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
                                Status = ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                                StatusCode = ((int?)responseMessage.StatusCode).ToString(),
                                Url = configRestAPI.Url,
                                Request = configRestAPI.Body,
                                Response = responseMessage.Content.ReadAsStringAsync().Result,
                                Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                                Reason = null,
                                CreatedDate = createDate,
                                ModificationDate = DateTime.Now
                            });

                            ResponseDataSuccess(configRestAPI.wfExecuteSuccess, responseMessage.Content.ReadAsStringAsync().Result, param, step.Param, step.Response, tenantId);
                        }
                    },
                    async (wfid, responseMessage) =>
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
                            StatusCode = ((int?)responseMessage.StatusCode).ToString(),
                            Url = configRestAPI.Url,
                            Request = configRestAPI.Body,
                            Response = responseMessage.Content.ReadAsStringAsync().Result,
                            Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                            Reason = null,
                            CreatedDate = createDate,
                            ModificationDate = DateTime.Now
                        });

                        if (!string.IsNullOrEmpty(configRestAPI.wfExecuteError))
                        {
                            ResponseDataError(configRestAPI.wfExecuteError, param, step.Param, step.Response, tenantId);
                        }
                    },
                     async (wfid, taskError) =>
                     {
                         if (!string.IsNullOrEmpty(configRestAPI.wfExecuteSuccess))
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
                                 StatusCode = "300",
                                 Url = configRestAPI.Url,
                                 Request = configRestAPI.Body,
                                 Response = null,
                                 Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                                 Reason = taskError,
                                 ModificationDate = DateTime.Now,
                                 CreatedDate = createDate
                             });

                             if (!string.IsNullOrEmpty(configRestAPI.wfExecuteError))
                             {
                                 ResponseDataError(configRestAPI.wfExecuteError, param, step.Param, step.Response, tenantId);
                             }
                         }
                     });

                    response2 = new RestAPIResponse()
                    {
                        Result = (step.Param == step.Response) ? JsonConvert.SerializeObject(param) : _modelControl.BindingDataDump(step.Response ?? "", tenantId).ToString(),
                        HttpResponse = new HttpResponseMessage()
                        {
                            StatusCode = HttpStatusCode.OK,
                        }
                    };
                }
                else
                {
                    response2 = this._restAPI.Send(new RestAPIRequestWFConfig
                    {
                        Url = configRestAPI.Url,
                        Body = configRestAPI.Body,
                        Method = configRestAPI.Method,
                        Headers = configRestAPI.Headers,
                        Timeout = configRestAPI.Timeout
                    }, tenantId);

                    HttpStatusCode[] allStatusCodes = (HttpStatusCode[])Enum.GetValues(typeof(HttpStatusCode));
                    var lstStatusCode = allStatusCodes.Where(code => ((int)code >= 200 && (int)code < 300)).ToList();

                    if (lstStatusCode != null && !lstStatusCode.Contains(response2.HttpResponse.StatusCode))
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
                            StatusCode = ((int?)response2.HttpResponse.StatusCode).ToString(),
                            Url = configRestAPI.Url,
                            Request = configRestAPI.Body,
                            Response = response2.Result,
                            Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                            Reason = null,
                            ModificationDate = DateTime.Now,
                            CreatedDate = createDate
                        });
                    }
                    else
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
                            Status = ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                            StatusCode = ((int?)response2.HttpResponse.StatusCode).ToString(),
                            Url = configRestAPI.Url,
                            Request = configRestAPI.Body,
                            Response = response2.Result,
                            Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                            Reason = null,
                            CreatedDate = createDate,
                            ModificationDate = DateTime.Now
                        });
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
                    StatusCode = ((int?)response2.HttpResponse.StatusCode).ToString(),
                    Url = configRestAPI.Url,
                    Request = configRestAPI.Body,
                    Response = response2.Result,
                    Headers = JsonConvert.SerializeObject(configRestAPI.Headers),
                    Reason = ex.Message,
                    CreatedDate = createDate,
                    ModificationDate = DateTime.Now
                });

                response2 = new RestAPIResponse()
                {
                    Result = (step.Param == step.Response) ? JsonConvert.SerializeObject(param) : _modelControl.BindingDataDump(step.Response ?? "", tenantId).ToString(),
                    HttpResponse = new HttpResponseMessage()
                    {
                        StatusCode = HttpStatusCode.RequestTimeout,
                        ReasonPhrase = ex.Message
                    }
                };
            }

            return response2;
        }

        private void ResponseDataSuccess(string wfid, string jsonResponse, object param, string? objId, string? objIdResponse, string tenantId)
        {
            try
            {
                var dataParam = this._modelControl.BindingData(objId, param, tenantId);
                var dataResponse = this._modelControl.BindingData(objIdResponse, jsonResponse, tenantId);

                Execute(wfid, JsonConvert.SerializeObject(new
                {
                    Request = dataParam,
                    Response = dataResponse,
                }), tenantId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ResponseDataError(string wfid, object param, string? objId, string? objIdResponse, string tenantId)
        {
            try
            {
                var dataParam = this._modelControl.BindingData(objId, param, tenantId);

                var dataResponse = this._modelControl.BindingDataDump(objIdResponse, tenantId);


                Execute(wfid, JsonConvert.SerializeObject(new
                {
                    Request = dataParam,
                    Response = dataResponse,
                }), tenantId);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Tìm kiếm các khu vực cần thay thế
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private List<string> findTag(string str)
        {
            List<string> strArr = str.Split(']').ToList();

            for (int i = 0; i < strArr.Count; ++i)
            {
                if (strArr[i].Contains('['))
                {
                    var spls = strArr[i].Split('[');
                    strArr[i] = spls[spls.Length - 1];
                }
                else
                {
                    strArr[i] = string.Empty;
                }
            }

            return strArr;
        }

        /// <summary>
        /// Thay thế giá trị trong chuỗi bằng giá trị được lưu tại cache
        /// </summary>
        /// <param name="objid"></param>
        /// <param name="strReplate"></param>
        /// <param name="fielName"></param>
        /// <param name="param"></param>
        /// <param name="isHTML"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        private string replacePropertyCache(string? objid, string strReplate, string fielName, object? param, string tenantId, bool isHTML = false)
        {
            try
            {
                if (param == null)
                {
                    return strReplate;
                }
                var test = param.GetType();

                if (typeof(String) == param.GetType())
                {
                    return strReplate.Replace(string.Format("[{0}]", objid), param.ToString());
                }

                if (param is IDictionary<string, object> dict)
                {
                    foreach (var kv in dict)
                    {
                        var key = kv.Key;
                        var val = kv.Value;

                        // Nếu value là dictionary lồng, đệ quy
                        if (val is IDictionary<string, object> innerDict)
                        {
                            strReplate = replacePropertyCache(objid, strReplate, key, innerDict, tenantId, isHTML);
                            continue;
                        }

                        var valStr = val switch
                        {
                            null => "null",
                            JToken j => j.ToString(),
                            _ => val.ToString()
                        };

                        strReplate = strReplate.Replace($"[{objid}.{key}]", valStr);
                        strReplate = strReplate.Replace($"[{objid}.{fielName}->{key}]", valStr);
                    }

                    return strReplate.Replace("\"null\"", "null");
                }

                var properties = param.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    string strTemp = strReplate;
                    var value = _modelControl.GetValue(param, prop.Name);

                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        strReplate = replacePropertyCache(objid, strReplate, prop.Name, value, tenantId);
                        if (strReplate == strTemp)
                        {
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}]", objid, prop.Name), JsonConvert.SerializeObject(value));
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}->{2}]", objid, fielName, prop.Name), JsonConvert.SerializeObject(value));
                        }
                    }
                    else
                    {
                        //*****2023-10-02 start update*****
                        //Update đẩy dạng list Models

                        if (prop.PropertyType.GenericTypeArguments.Count() > 0)
                        {
                            Type typeList = typeof(List<>).MakeGenericType(prop.PropertyType.GenericTypeArguments.FirstOrDefault());
                            if (value?.GetType() == typeList)
                            {
                                var data = value;
                                var paramdata = JsonConvert.SerializeObject(data);
                                strReplate = strReplate.Replace(string.Format("[{0}.{1}]", objid, prop.Name), paramdata);
                                strReplate = strReplate.Replace(string.Format("[{0}.{1}->{2}]", objid, fielName, prop.Name), paramdata);
                            }
                        }

                        if (isHTML && !prop.PropertyType.GenericTypeArguments.Any())
                        {
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}]", objid, prop.Name), value != null ? value?.ToString() : "null");
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}->{2}]", objid, fielName, prop.Name), value != null ? value?.ToString() : "null");
                        }
                        else
                        {
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}]", objid, prop.Name), value != null ? value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") : "null");
                            strReplate = strReplate.Replace(string.Format("[{0}.{1}->{2}]", objid, fielName, prop.Name), value != null ? value.ToString().Replace("\"", "\\\"").Replace("\n", "\\n") : "null");
                        }

                        //*******2023-10-02 End Update*******
                    }
                }

                return strReplate.Replace("\"null\"", "null");
            }
            catch (Exception ex)
            {
                throw new IboxLog($"ReplatePropertyCache Exception \n objid: {objid}, requestBody: {strReplate}, param: {param} \n Message: {ex}", tenantId);
            }
        }

        /// <summary>
        /// Thay thế giá trị trong chuỗi
        /// </summary>
        /// <param name="strReplate">Chuỗi định dạng</param>
        /// <param name="fieldName">Tên trường đứng trước</param>
        /// <param name="param">Giá trị đứng sau</param>
        /// <param name="isHTML">Phân loại là html thì không xử lý loại bỏ ký tự đặc biệt</param>
        /// <returns></returns>
        private string replaceProperty(string? strReplate, string fieldName, object param, bool isHTML = false)
        {
            try
            {
                if (param == null)
                {
                    return strReplate;
                }

                var properties = param.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    string strTemp = strReplate;
                    var value = _modelControl.GetValue(param, prop.Name);
                    if (value == null)
                    {
                        value = null;
                    }

                    if (prop.PropertyType.Assembly.IsDynamic)
                    {
                        strReplate = replaceProperty(strReplate, prop.Name, value);

                        if (strReplate == strTemp)
                        {
                            var valueJson = JsonConvert.SerializeObject(value);
                            strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), valueJson);
                            strReplate = strReplate.Replace(string.Format("[{0}->{1}]", fieldName, prop.Name), valueJson);
                        }
                    }
                    else
                    {
                        //*****2023-10-02 start update*****
                        //Update đẩy dạng list Models
                        if (prop.PropertyType.GenericTypeArguments.Count() > 0)
                        {
                            Type typeList = typeof(List<>).MakeGenericType(new[] { prop.PropertyType.GenericTypeArguments[0] });
                            if (value?.GetType() == typeList)
                            {
                                var paramdata = JsonConvert.SerializeObject(value);
                                strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), paramdata);
                                strReplate = strReplate.Replace(string.Format("[{0}->[1]]", fieldName, prop.Name), paramdata);
                            }
                            else
                            {
                                var paramdata = JsonConvert.SerializeObject(value);
                                if (!string.IsNullOrEmpty(paramdata) && paramdata != "null")
                                {
                                    strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), paramdata);
                                    strReplate = strReplate.Replace(string.Format("[{0}->{1}]", fieldName, prop.Name), paramdata);
                                }
                                else
                                {
                                    strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), "null");
                                    strReplate = strReplate.Replace(string.Format("[{0}->{1}]", fieldName, prop.Name), "null");
                                }
                            }
                        }
                        else
                        {
                            if (isHTML)
                            {
                                strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), value != null ? value?.ToString() : "null");
                                strReplate = strReplate.Replace(string.Format("[{0}->{1}]", fieldName, prop.Name), value != null ? value?.ToString() : "null");
                            }
                            else
                            {
                                var valueAffter = value != null ? value?.ToString()?.Replace("\"", "\\\"").Replace("\n", "\\n") : "null";
                                strReplate = strReplate.Replace(string.Format("[{0}]", prop.Name), valueAffter);
                                strReplate = strReplate.Replace(string.Format("[{0}->{1}]", fieldName, prop.Name), valueAffter);
                            }
                        }
                        //*******2023-10-02 End Update*******
                    }
                }

                return strReplate.Replace("\"null\"", "null"); ;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic AddEnd(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                AddEnd addEndConfig = GetConfig<AddEnd>(step);
                if (addEndConfig.Value == null)
                {
                    throw new IboxLog("Value of list is not have any data", tenantId);
                }

                addEndConfig.Value = replaceProperty(addEndConfig.Value ?? "", "", param);
                List<string> strarr7 = findTag(addEndConfig.Value);

                strarr7.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        addEndConfig.Value = replacePropertyCache(objid, addEndConfig.Value, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                if (string.IsNullOrEmpty(addEndConfig.FieldName))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                if (step.SaveResponseToCache ?? false)
                {


                    if (caches.TryGetValue(step.Id, out WFCache cachess))
                    {

                        var fieldValue = this._modelControl.GetValue(cachess.Obj, addEndConfig.FieldName);

                        if (fieldValue != null)
                        {
                            string dataNew = (string)(this._modelControl.GetValue(param, addEndConfig.FieldName) ?? "");
                            string dataOld = $"{addEndConfig.Value}{fieldValue}";

                            this._modelControl.SetValue(param, addEndConfig.FieldName, _iString.AddEnd(dataNew, dataOld));
                        }
                        else
                        {
                            this._modelControl.SetValue(param, addEndConfig.FieldName, $"{fieldValue}{addEndConfig.Value}");
                        }
                    }


                }
                else
                {
                    var fieldValue = this._modelControl.GetValue(param, addEndConfig.FieldName);
                    this._modelControl.SetValue(param, addEndConfig.FieldName, $"{fieldValue}{addEndConfig.Value}");
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = param,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var res1 = LoadStep(step.ChildSteps, param, wfName, tenantId);

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = res1
                });

                return res1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic Replate(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                Replate replateConfig = GetConfig<Replate>(step);
                if (string.IsNullOrEmpty(replateConfig.ValueSource))
                {
                    throw new IboxLog("Value source is not have any data", tenantId);
                }

                replateConfig.ValueDestination = replaceProperty(replateConfig.ValueDestination ?? "", "", param);
                List<string> strarr8 = findTag(replateConfig.ValueDestination);

                strarr8.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        replateConfig.ValueDestination = replacePropertyCache(objid, replateConfig.ValueDestination, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                if (string.IsNullOrEmpty(replateConfig.FieldName))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                this._modelControl.SetValue(param, replateConfig.FieldName, _iString.Replace((string)(this._modelControl.GetValue(param, replateConfig.FieldName) ?? ""), replateConfig.ValueSource, replateConfig.ValueDestination));

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = param,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var res2 = LoadStep(step.ChildSteps, param, wfName, tenantId);

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = res2
                });

                return res2;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic SplitToObject(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                SplitToObject splitToObject = GetConfig<SplitToObject>(step);

                splitToObject.ModelJsonMappingForSplit = replaceProperty(splitToObject.ModelJsonMappingForSplit ?? "", "", param);
                List<string> strarr9 = findTag(splitToObject.ModelJsonMappingForSplit);

                strarr9.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        splitToObject.ModelJsonMappingForSplit = replacePropertyCache(objid, splitToObject.ModelJsonMappingForSplit, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                if (string.IsNullOrEmpty(splitToObject.FieldName))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                string valueOfProperty = (string)(this._modelControl.GetValue(param, splitToObject.FieldName) ?? "");

                if (string.IsNullOrEmpty(valueOfProperty))
                {
                    throw new IboxLog("Can not read data from data null", tenantId);
                }

                int i = 0;
                foreach (var item in valueOfProperty.ToString().Split(splitToObject.CharValueForSplit))
                {
                    splitToObject.ModelJsonMappingForSplit = splitToObject.ModelJsonMappingForSplit.Replace(@"[" + i + "]", item.ToString());
                    i = i + 1;
                }

                var dataResponse8 = _modelControl.BindingData(step.Response, splitToObject.ModelJsonMappingForSplit, tenantId);

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = dataResponse8,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var res3 = LoadStep(step.ChildSteps, dataResponse8, wfName, tenantId);

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = res3
                });

                return res3;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic ListDataToListObject(WFStep step, object param, string wfName, string tenantId)
        {
            try
            {
                ListToObject listToObjectConfig = GetConfig<ListToObject>(step);

                listToObjectConfig.ModelJsonMappingForListToListObject = replaceProperty(listToObjectConfig.ModelJsonMappingForListToListObject ?? "", "", param);
                List<string> strarr10 = findTag(listToObjectConfig.ModelJsonMappingForListToListObject);

                strarr10.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr))
                    {
                        var objid = ptr.Split('.')[0];
                        var cache = this.caches.FirstOrDefault(ptr => ptr.Key == objid);
                        listToObjectConfig.ModelJsonMappingForListToListObject = replacePropertyCache(objid, listToObjectConfig.ModelJsonMappingForListToListObject, "", cache.Value?.Obj ?? new { }, tenantId);
                    }
                });

                object dataResponse9 = new { };
                Type type1 = _modelControl.GetType(step.Response, tenantId);

                var response3 = _modelControl.BindingDataDump(step.Response, tenantId);



                object resForType = JsonConvert.DeserializeObject(response3.ToString(), type1);



                if (string.IsNullOrEmpty(listToObjectConfig.FieldName))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                var valueOfProperty = this._modelControl.GetValue(param, listToObjectConfig.FieldName);

                if (valueOfProperty == null)
                {
                    throw new IboxLog("Can not read data from data null", tenantId);
                }

                if (valueOfProperty.GetType().GetGenericArguments().Length == 0)
                {
                    throw new IboxLog("field name hasn't type is list", tenantId);
                }

                foreach (var item in (IList)valueOfProperty)
                {
                    dataResponse9 = _modelControl.BindingDataToProperty(type1,
                                                                        resForType,
                                                                        listToObjectConfig.FieldNameDestination,
                                                                        listToObjectConfig.ModelJsonMappingForListToListObject.Replace(@"[]", item.ToString()), false, tenantId);
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = dataResponse9,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var res4 = LoadStep(step.ChildSteps, dataResponse9, wfName, tenantId);

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = res4
                });

                return res4;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private dynamic? validDataForFindInList(object item, FindConfig config)
        {
            if (item == null || config == null)
            {
                return null;
            }

            return item.GetType().GetProperties()
                                .FirstOrDefault(ptr =>
                                {
                                    if (ptr == null)
                                    {
                                        return false;
                                    }

                                    try
                                    {
                                        var value = _modelControl.GetValue(item, ptr.Name);
                                        if (value == null)
                                        {
                                            return false;
                                        }


                                        if (value.ToString().Contains(config.FindWithValue))
                                        {
                                            return true;
                                        }

                                    }
                                    catch
                                    {
                                        return false;
                                    }

                                    return false;
                                });
        }

        private dynamic AutoGenerateData(WFStep step, object param, AutoGenerateDataType autoGenerateDataType, string wfName, string tenantId)
        {
            try
            {
                AddEnd formatingConfig = GetConfig<AddEnd>(step);
                if (string.IsNullOrEmpty(formatingConfig.FieldName))
                {
                    throw new IboxLog("Field name must be set", tenantId);
                }

                switch (autoGenerateDataType)
                {
                    case AutoGenerateDataType.Number:
                        this._modelControl.SetValue(param, formatingConfig.FieldName, _iString.RandomNumber());
                        break;

                    case AutoGenerateDataType.GUID:
                        this._modelControl.SetValue(param, formatingConfig.FieldName, _iString.GetGUID());
                        break;

                    case AutoGenerateDataType.Datetime:
                    default:
                        this._modelControl.SetValue(param, formatingConfig.FieldName, _iString.GetDateTime(formatingConfig.Value ?? "yyyy-MM-dd HH:mm:ss"));
                        break;
                }

                saveDebug(new ModelXWorkflowDebug
                {
                    RequestBody = param,
                    ResponseBody = param,
                    StepID = step.Id,
                    StepName = step.Description,
                    ErrorMessage = string.Empty,
                    WorkflowId = step.WfId
                });

                var res1 = LoadStep(step.ChildSteps, param, wfName, tenantId);

                saveCahe(step.Id ?? throw new IboxLog("step id is null", tenantId), new WFCache()
                {
                    ObjType = this._modelControl.GetType(step.Response, tenantId),
                    Obj = res1
                });

                return res1;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion Xử lý nghiệp vụ cho từng loại thực hiện
    }

    public enum AutoGenerateDataType
    {
        Number,
        GUID,
        Datetime
    }

    public class BinaryConfig
    {
        public string? FieldName { get; set; }        // tên field chứa file trong form-data/param
        public string? FileName { get; set; }         // nếu cần override file name
        public string? ContentType { get; set; }      // mime type mặc định
    }
}