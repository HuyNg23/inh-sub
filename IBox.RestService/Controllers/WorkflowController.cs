using Azure.Core;
using DocumentFormat.OpenXml.Office2010.Excel;
using IBox.Common.FolderLog;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using IBox.DLEx.Implementation;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Serilog;
using System.Text;

namespace IBox.RestService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly IExecuteWF _executeWF;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IBContext<TenantContext> tenantContext;
        private readonly IRestAPI _restAPI;
        private readonly IRestLog _restLog;

        public WorkflowController(IRestAPI restAPI, IExecuteWF executeDLL, IBContext<TenantContext> tenantContext, Common.Objects.IConfiguration configuration, IRestLog restLog)
        {
            _restAPI = restAPI;
            _executeWF = executeDLL;
            this.tenantContext = tenantContext;
            _configuration = configuration;
            _restLog = restLog;
        }

        [HttpPost]
        [HttpGet]
        [Route("Execute/{tenantId}/{id}")]
        public async Task<dynamic> Execute(string tenantId, string id)
        {
            this._executeWF.HttpResponse = this.Response;
            string message = string.Empty;
            string KeyExecuteRunWorkFlow = Guid.NewGuid().ToString();
            DateTime createDate = DateTime.Now;
            try
            {
                if (!Response.HasStarted)
                {
                    var address = IBGlobalConfig.ThisSite.Split('.').Last();
                    Response.Headers.TryAdd("IBox", address);
                }

                if (Request.Method == "GET")
                {
                    message = BuildJsonFromQuery(Request.Query);
                }
                else
                {
                    message = await ReadRequestBodyAsync(Request);
                }

                var (result, wfName1) = this._executeWF.CheckDeploy(id, this.tenantContext, tenantId)
                                        .AddHeader(this.Request.Headers)
                                        .Execute(id, message, tenantId);

                new IboxLog($"API was called workflowID: {id}\n request: {message} \n response: {JsonConvert.SerializeObject(result)}", tenantId, "Information");

                this.Response.StatusCode = this._executeWF.HttpResponse.StatusCode;

                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = null,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = message,
                    ResultWF = JsonConvert.SerializeObject(result),
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Complete.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate,
                    WFName = wfName1
                });

                return result;
            }
            catch (Exception ex)
            {
                new IboxLog($"Error API workflowID: {id}\n request: {message} \n Exception: {ex}", tenantId);

                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = ex.Message,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = message,
                    ResultWF = null,
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Fail.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate
                });

                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Thông báo có 1 Workflow lỗi khi chạy",
                    MailBody = $"Exception: {ex.Message}",
                    WFID = id,
                    typeWarning = TypeWarning.WorkflowError
                });

                this.Response.StatusCode = 500;
                return new
                {
                    Code = "1",
                    Message = ex.Message
                };
            }
        }

        [HttpPost]
        [Route("Execute/upload/{tenantId}/{id}")]
        public async Task<dynamic> ExecuteUploadFile(string tenantId, string id)
        {
            this._executeWF.HttpResponse = this.Response;
            var param = new Dictionary<string, object>();
            string KeyExecuteRunWorkFlow = Guid.NewGuid().ToString();
            DateTime createDate = DateTime.Now;

            try
            {
                if (!Response.HasStarted)
                {
                    var address = IBGlobalConfig.ThisSite.Split('.').Last();
                    Response.Headers.TryAdd("IBox", address);
                }

                if (Request.HasFormContentType)
                {
                    var form = await Request.ReadFormAsync();
                    var files = form.Files;
                    var dict = new Dictionary<string, object>();

                    foreach (var kv in form)
                    {
                        dict[kv.Key] = kv.Value.ToString();
                    }

                    // Chỉ cho phép các định dạng file cụ thể
                    var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                        ".doc", ".docx", ".xls", ".xlsx", ".pdf", ".mp4"
                    };

                    foreach (var file in files)
                    {
                        var extension = Path.GetExtension(file.FileName);
                        if (!allowedExtensions.Contains(extension))
                        {
                            return new ResponseForm<dynamic>(() => new
                            {
                                Code = "1",
                                Message = $"File type '{extension}' is not allowed."
                            });
                        }
                    }

                    dict["__files"] = files;
                    param = dict;
                }
                else
                {
                    return new ResponseForm<dynamic>(() => new
                    {
                        Code = "1",
                        Message = "File not found."
                    });
                }

                var (result, wfName1) = this._executeWF.CheckDeploy(id, this.tenantContext, tenantId)
                                    .AddHeader(this.Request.Headers)
                                    .Execute(id, param, tenantId);

                this.Response.StatusCode = this._executeWF.HttpResponse.StatusCode;

                // Chuẩn bị dữ liệu log (bỏ binary file, chỉ ghi metadata)
                var logParam = new Dictionary<string, object>(param);
                if (logParam.ContainsKey("__files"))
                {
                    var fileInfos = ((IFormFileCollection)logParam["__files"])
                        .Select(f => new { f.FileName, f.Length, f.ContentType });
                    logParam["__files"] = fileInfos;
                }

                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = null,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = JsonConvert.SerializeObject(logParam),
                    ResultWF = JsonConvert.SerializeObject(result),
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Complete.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate,
                    WFName = wfName1
                });

                return result;
            }
            catch (Exception ex)
            {
                new IboxLog($"Error API workflowID: {id}\n request: {JsonConvert.SerializeObject(param)} \n Exception: {ex}", tenantId);

                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = ex.Message,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = JsonConvert.SerializeObject(param),
                    ResultWF = null,
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Fail.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate
                });

                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Thông báo có 1 Workflow lỗi khi chạy",
                    MailBody = $"Exception: {ex.Message}",
                    WFID = id,
                    typeWarning = TypeWarning.WorkflowError
                });

                this.Response.StatusCode = 500;
                return new
                {
                    Code = "1",
                    Message = ex.Message
                };
            }
        }

        private string BuildJsonFromQuery(IQueryCollection parameters)
        {
            try
            {
                string obj = string.Empty;
                if (parameters.Any())
                {
                    var jsonBuilder = new StringBuilder();
                    foreach (var param in parameters)
                    {
                        jsonBuilder.AppendFormat("\"{0}\":\"{1}\",", param.Key, param.Value);
                    }

                    if (jsonBuilder.Length > 0)
                    {
                        jsonBuilder.Length--;
                    }

                    obj = $"{{{jsonBuilder}}}";
                }
                else
                {
                    obj = "{}";
                }

                return obj;
            }
            catch (Exception ex)
            {
                new IboxLog($"BuildJsonFromQuery Exception: {ex}", "AppLogs");
                return "{}";
            }
        }
        public async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                if (!request.Body.CanSeek)
                {
                    request.EnableBuffering();
                }

                request.Body.Position = 0;

                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var message = await reader.ReadToEndAsync();

                request.Body.Position = 0;

                return message;
            }
            catch (Exception ex)
            {
                new IboxLog($"ReadRequestBodyAsync Exception: {ex}", "AppLogs");
                throw;
            }
        }

        [HttpPost]
        [HttpGet]
        [Route("ExecuteWH/{tenantId}/{id}")]
        public async Task ExecuteWH(string tenantId, string id)
        {
            string message = string.Empty;
            string KeyExecuteRunWorkFlow = Guid.NewGuid().ToString();
            DateTime createDate = DateTime.Now;
            try
            {
                this._executeWF.HttpResponse = this.Response;
                if (!Response.HasStarted)
                {
                    var address = IBGlobalConfig.ThisSite.Split('.').Last();
                    Response.Headers.TryAdd("IBox", address);
                }

                message = await ReadRequestBodyAsync(Request);

                HttpContext.Response.CompleteAsync();

                var (result, nameWF) = this._executeWF
                    .CheckDeploy(id, this.tenantContext, tenantId)
                    .AddHeader(this.Request.Headers)
                    .Execute(id, message, tenantId);

                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = null,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = message,
                    ResultWF = JsonConvert.SerializeObject(result),
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Complete.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate,
                    WFName = nameWF
                });

                Log.Information($"APIWH was called workflowID: {id}\n request: {message} \n resultPost: {JsonConvert.SerializeObject(result)}");
            }
            catch (Exception ex)
            {
                _restLog.WriteFileLogWF(IBGlobalConfig.ServiceName, new WFModel()
                {
                    KeyExecuteRunWorkFlow = KeyExecuteRunWorkFlow,
                    ModificationDate = DateTime.Now,
                    Reason = ex.Message,
                    Header = JsonConvert.SerializeObject(this.Request.Headers),
                    RequestWF = message,
                    ResultWF = null,
                    SiteRun = IBGlobalConfig.ThisSite,
                    Status = WorkflowExecuteStatus.Fail.ToString(),
                    TenantId = tenantId,
                    Wfid = id,
                    CreatedDate = createDate
                });

                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantId,
                    MailTitle = "[Warning] Thông báo có 1 WorkflowWH bị lỗi khi chạy",
                    WFID = id,
                    typeWarning = TypeWarning.WorkflowWHError
                });

                new IboxLog($"ExecuteWH Exception: {ex}", tenantId);
            }
        }

        [HttpPost("Deploy/{id}")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-deploy")]
        public ResponseForm<dynamic> DeployWF(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).Deploy(id, this.Request, ViewDeployServiceType.Rest));
        }

        [HttpPost("DeployHA/{id}")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-deploy")]
        public ResponseForm<dynamic> DeployWFHA(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).Deploy(id, tenantID));
        }

        [HttpPost("Recovery/{id}")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-unlock")]
        public ResponseForm<dynamic> RecoveryWF(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).Recovery(id, this.Request, ViewDeployServiceType.Rest));
        }

        [HttpPost("RecoveryHA/{id}")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-unlock")]
        public ResponseForm<dynamic> RecoveryWFHA(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).Recovery(id));
        }

        [HttpPost("ViewWorkflowDeploy")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view")]
        public ResponseForm<dynamic> ViewWorkflowDeploy()
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).ViewWFDeploy(this.Request, ViewDeployServiceType.Rest));
        }

        [HttpPost("ViewWorkflowDeployHA")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view")]
        public ResponseForm<dynamic> ViewWorkflowDeployHA()
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => this._executeWF.SetTenantContext(this.tenantContext, tenantID).ViewWFDeploy());
        }
    }
}