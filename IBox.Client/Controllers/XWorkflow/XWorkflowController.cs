using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using IBox.Security;
using IBox.Workflow.Execution;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Dynamic;
using System.Text.Json;

namespace IBox.Client.Controllers.XWorkflow
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class XWorkflowController : ActionController<WF_Define, TenantContext>
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IExecuteWF executeWF;
        private readonly IRestAPI restAPI;
        private readonly IWorkflowControl workflowControl;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public XWorkflowController(IServiceProvider serviceProvider,
            IBContext<TenantContext> tenantContext,
            IExecuteWF executeWF,
            IEncryption encryption,
            IRestAPI restAPI,
            IWorkflowControl workflowControl,
            Common.Objects.IConfiguration configuration) : base(tenantContext, encryption)
        {
            this.serviceProvider = serviceProvider;
            this.executeWF = executeWF;
            this.restAPI = restAPI;
            this.workflowControl = workflowControl;
            _configuration = configuration;
            _encryption = encryption;
        }

        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view", "integration-config-workflow-view-mySelf")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view", "integration-config-workflow-view-mySelf")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-manage", "integration-config-workflow-manage-mySelf")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var userId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.UserId.ToString()).Value.ToString() ?? string.Empty;

                var wfModel = CheckValid(req);

                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString();

                if (string.IsNullOrEmpty(wfModel.Name?.Trim()))
                {
                    throw new IboxLog("Name can't null or Empty.", tenantId ?? "AppLogs");
                }

                wfModel.CreatedById = userId;
                req.Body = JsonDocument.Parse(JsonConvert.SerializeObject(wfModel)).RootElement;

                var data = base.IBContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Name == wfModel.Name && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Name already exists.", tenantId ?? "AppLogs");
                }

                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-manage-mySelf")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var wfModel = CheckValid(req);

                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString();
               
                if (string.IsNullOrEmpty(wfModel.Name?.Trim()))
                {
                    throw new IboxLog("Name can't null or Empty.", tenantId ?? "AppLogs");
                }

                var data = base.IBContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Name == wfModel.Name && ptr.Id != wfModel.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Name already exists.", tenantId ?? "AppLogs");
                }

                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-manage-mySelf")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        /// <summary>
        /// Lấy chi tiết workflow
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("GetWFByID")]
        [IBoxActionPermission("integration-config-workflow-view", "integration-config-workflow-manage")]
        public ResponseForm<WFDefine> GetWFByID(RequestForm<BWorkflow> req)
        {
            return new ResponseForm<WFDefine>(() =>
            {
                return req.Body.SetTenantContext(IBContext).Init(serviceProvider).GetByID();
            });
        }

        /// <summary>
        /// Debug workflow
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("Debug/{id}")]
        [IBoxActionPermission("integration-config-workflow-test", "integration-config-workflow-manage")]
        public ResponseForm<dynamic> Debug(string id, RequestForm<dynamic> req)
        {
            try
            {
                if (!Request.Body.CanSeek)
                {
                    Request.EnableBuffering();
                }

                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                var result = executeWF.SetTenantContext(IBContext)
                                                     .AddHeader(Request.Headers)
                                                     .ExecuteDebug(id, req.Body, tenantID);

                return new ResponseForm<dynamic>(() => result.Debug);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }

        [HttpPost("DebugUpload/{id}")]
        [IBoxActionPermission("integration-config-workflow-test", "integration-config-workflow-manage")]
        [RequestSizeLimit(26_214_400)]
        public async Task<ResponseForm<dynamic>> Debug(string id)
        {
            try
            {
                dynamic param;

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

                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                var result = executeWF.SetTenantContext(IBContext)
                                      .AddHeader(Request.Headers)
                                      .ExecuteDebug(id, param, tenantId);

                return new ResponseForm<dynamic>(() => result.Debug);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpGet("UseWF/{id}")]
        [IBoxActionPermission("integration-config-workflow-test", "integration-config-workflow-manage")]
        public ResponseForm<dynamic> UseWF(string id)
        {
            try
            {
                var result = IBContext.Context.WF_Steps.Where(ws => ws.Type == WF_Type.RecallWF && ws.Config.Contains(id))
                    .Join(
                    IBContext.Context.WF_Defines,
                    ws => ws.WFid,
                    wd => wd.Id,
                    (ws, wd) => new { wd.Id, wd.Name }
                    );

                return new ResponseForm<dynamic>(() => result.ToList());
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    Message = ex.Message
                });
            }
        }

        /// <summary>
        /// Debug workflow sử dụng cho page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("DebugWorkflow/{id}")]
        [IBoxActionPermission("integration-config-page-manage")]
        public ResponseForm<dynamic> DebugWorkflow(string id, RequestForm<dynamic> req)
        {
            try
            {
                if (!Request.Body.CanSeek)
                {
                    Request.EnableBuffering();
                }

                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                var result = executeWF.SetTenantContext(IBContext).AddHeader(Request.Headers).ExecuteDebug(id, req.Body, tenantID);

                return new ResponseForm<dynamic>(() => result.DebugResult);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    ex.Message
                });
            }
        }

        [HttpPost("Diagram/{id}")]
        public ResponseForm<dynamic> Diagram(string id, RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                try
                {
                    if (!Request.Body.CanSeek)
                    {
                        Request.EnableBuffering();
                    }
                    return workflowControl.SetTenantContext(IBContext);
                }
                catch (Exception ex)
                {
                    return new
                    {
                        Code = "1",
                        ex.Message
                    };
                }
            });
        }

        /// <summary>
        /// Mở khóa workflow
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [HttpPost("Unlock/{id}")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-unlock")]
        public ResponseForm<dynamic> Unlock(string id, RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                try
                {
                    var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString();

                    var wF_Defines = IBContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == id);
                    if (wF_Defines == null)
                    {
                        throw new IboxLog("Workflow ID not exist.", tenantID ?? "AppLogs");
                    }

                    wF_Defines.TryUpdate(IBContext.Context, new WF_Define()
                    {
                        Id = id,
                        IsLock = false
                    });
                    this.restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = tenantID,
                        MailTitle = "[Warning] Thông báo có 1 API được mở khóa",
                        WFID = id,
                        typeWarning = TypeWarning.APIUnlock
                    });

                    return new
                    {
                        Code = "0",
                        Message = "Success"
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        Code = "1",
                        ex.Message
                    };
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    executeWF.Dispose();
                    restAPI.Dispose();
                    workflowControl.Dispose();
                }

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}