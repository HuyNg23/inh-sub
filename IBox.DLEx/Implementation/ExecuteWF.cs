using IBox.Common.FolderLog;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.DBC.Factories;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using IBox.DLEx.Model;
using IBox.Formatting;
using IBox.MailService;
using IBox.Workflow.Execution;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System.Dynamic;
using System.Text;

namespace IBox.DLEx.Implementation
{
    public partial class ExecuteWF : ATenantContext<ExecuteWF>, IExecuteWF
    {
        private readonly IModelControl _modelControl;
        private readonly IWorkflowControl _workflowControl;
        private IHeaderDictionary _headerDictionary { set; get; }
        private readonly IConfiguration _configuration;
        private readonly IRestAPI _restAPI;
        private readonly IRestLog _restLog;
        private readonly IDBConnection _connection;
        private readonly ISmtpService _smtp;
        private readonly IEncryption _encryption;
        private bool debugFlag = false;
        private readonly Dictionary<string, WFCache> caches = new Dictionary<string, WFCache>();
        private readonly IString _iString;
        private HttpResponse httpResponse;
        public HttpResponse HttpResponse { get => httpResponse; set => httpResponse = value; }

        public List<ModelXWorkflowDebug> Debug
        {
            get
            {
                return debug;
            }
        }

        public object DebugResult
        {
            get
            {
                return debugResult;
            }
        }

        public ExecuteWF(IServiceProvider serviceProvider)
        {
            _workflowControl = serviceProvider.GetService<IWorkflowControl>();
            _modelControl = serviceProvider.GetService<IModelControl>();
            _configuration = serviceProvider.GetService<IConfiguration>();
            _restAPI = serviceProvider.GetService<IRestAPI>();
            _connection = serviceProvider.GetService<IDBConnection>();
            _smtp = serviceProvider.GetService<ISmtpService>();
            _iString = serviceProvider.GetService<IString>();
            _encryption = serviceProvider.GetService<IEncryption>();
            _restLog = serviceProvider.GetService<IRestLog>();
        }

        public IExecuteWF AddHeader(IHeaderDictionary headers)
        {
            this._headerDictionary = headers;
            return this;
        }

        protected override void ImplementationDIInLocalObject()
        {
            if (this.tenantContext == null)
            {
                throw new IboxLog("Tenant context have not aviable", "AppLogs");
            }

            this._modelControl.SetTenantContext(this.tenantContext);
            this._workflowControl.SetTenantContext(this.tenantContext);
        }

        public override ExecuteWF SetTenantContext(IBContext<TenantContext> tenantContext)
        {
            base.SetTenantContext(tenantContext);
            this.ImplementationDIInLocalObject();
            return this;
        }

        public override ExecuteWF SetTenantContext(IBContext<TenantContext> tenantContext, string tenantID)
        {
            base.SetTenantContext(tenantContext, tenantID);
            this.ImplementationDIInLocalObject();
            return this;
        }

        public ExecuteWF CheckDeploy(string wfid, IBContext<TenantContext> tenantContext, string tenantID)
        {
            if (!_workflowControl.IsExist(wfid, tenantID))
            {
                base.SetTenantContext(tenantContext, tenantID);
                this.ImplementationDIInLocalObject();
                _workflowControl.Deploy(wfid, tenantID);
            }

            return this;
        }

        public (dynamic, string) Execute(string wfid, dynamic param, string tenantId)
        {
            try
            {
                Log.Information(string.Format("Start Execute wID: {0} at {1}", wfid, DateTime.Now));

                if (string.IsNullOrEmpty(wfid))
                {
                    throw new IboxLog("work flow id is required", tenantId);
                }

                if (string.IsNullOrEmpty(param.ToString()))
                {
                    throw new IboxLog("parameter is required", tenantId);
                }

                var wf = _workflowControl.GetWFDeployByID(wfid, tenantId);

                if (wf.AuthenType == AuthorType.Basic)
                {
                    if (this._headerDictionary.Any(ptr => ptr.Key == "Authorization"))
                    {
                        string encoded = System.Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1")
                                   .GetBytes(wf.UserName + ":" + wf.Password));

                        encoded = "Basic " + encoded;

                        var authorizationValue = this._headerDictionary.FirstOrDefault(ptr => ptr.Key == "Authorization");
                        if (authorizationValue.Value != encoded)
                        {
                            throw new IboxLog("Authorization is failse", tenantId);
                        }
                    }
                    else
                    {
                        throw new IboxLog("Authorization is required", tenantId);
                    }
                }

                var result = LoadStep(wf.WFstep, param, wf.Name, tenantId);
                return (result, wf.Name);
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        public (dynamic, string) Execute(string wfid, dynamic param, string tenantId, bool isRedeploy)
        {
            try
            {
                if (!_workflowControl.IsExist(wfid, tenantId))
                {
                    _workflowControl.Deploy(wfid, tenantId);
                }

                return Execute(wfid, param, tenantId);
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        private List<ModelXWorkflowDebug> debug = new List<ModelXWorkflowDebug>();
        private object debugResult;

        public ExecuteWF ExecuteDebug(string wfid, dynamic param, string tenantId)
        {
            try
            {
                debug = new List<ModelXWorkflowDebug>();
                this.debugFlag = true;
                Deploy(wfid, tenantId);
                debugResult = Execute(wfid, param, tenantId);
                this.debugFlag = false;
                var sortedDebugList = debug.OrderBy(ptr => ptr.StartDate).ToList();
                this.debug = sortedDebugList;
                return this;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        private dynamic LoadStep(List<WFStep>? wFstep, object param, string wfName, string tenantId, IFormFileCollection? uploadedFiles = null)
        {
            if (wFstep == null || !wFstep.Any())
            {
                return param;
            }

            // Nếu param chứa __files thì cập nhật uploadedFiles (phát hiện 1 lần khi param ban đầu chứa files)
            try
            {
                if (uploadedFiles == null) // chỉ lấy từ param khi chúng ta chưa có uploadedFiles từ trước
                {
                    if (param is ExpandoObject expando)
                    {
                        var dict = (IDictionary<string, object>)expando;
                        if (dict.ContainsKey("__files") && dict["__files"] != null)
                        {
                            uploadedFiles = dict["__files"] as IFormFileCollection;
                            if (uploadedFiles == null && dict["__files"] is IEnumerable<IFormFile> listFiles)
                            {
                                var coll = new FormFileCollection();
                                foreach (var f in listFiles) coll.Add(f);
                                uploadedFiles = coll;
                            }
                        }
                    }
                    else if (param is IDictionary<string, object> dictObj)
                    {
                        if (dictObj.ContainsKey("__files") && dictObj["__files"] != null)
                        {
                            uploadedFiles = dictObj["__files"] as IFormFileCollection;
                            if (uploadedFiles == null && dictObj["__files"] is IEnumerable<IFormFile> listFiles)
                            {
                                var coll = new FormFileCollection();
                                foreach (var f in listFiles) coll.Add(f);
                                uploadedFiles = coll;
                            }
                        }
                    }
                }
            }
            catch
            {
                // swallow — we'll continue with whatever uploadedFiles currently is (null or a collection)
            }

            foreach (var step in wFstep)
            {
                try
                {
                    if (string.IsNullOrEmpty(step.Param))
                    {
                        throw new IboxLog(string.Format("parameter in step {0} is null or empty", step.Id), tenantId);
                    }

                    if (!this._modelControl.IsObjExist(step.Param, tenantId))
                    {
                        Log.Information("Check LoadStep Schedule", step.Param);
                        var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                        var tenantContext1 = context.GetTenantContext(tenantId).Context;
                        this._modelControl.BuildModel(step.Param, tenantId, tenantContext1);
                        tenantContext1.Context.Dispose();
                    }

                    switch (step.Type)
                    {
                        case WF_Type.Start:
                            var resultStart = runStart(step, param, tenantId);
                            // truyền uploadedFiles xuống lần gọi đệ quy tiếp theo (giữ nguyên nếu null)
                            return LoadStep(step.ChildSteps, resultStart, wfName, tenantId, uploadedFiles);

                        case WF_Type.ForwardFile:
                            var (resultForwardFile, childStepsForwardFile) = runForwardFileStep(step, param, wfName, tenantId, uploadedFiles);
                            return LoadStep(childStepsForwardFile, resultForwardFile, wfName, tenantId, uploadedFiles);

                        case WF_Type.CallXAPI:
                            var (resultXApi, childStepsXApi) = runCallXAPI(step, param, wfName, tenantId, uploadedFiles);
                            return LoadStep(childStepsXApi, resultXApi, wfName, tenantId, uploadedFiles);

                        case WF_Type.RunScript:
                            var (resultRunScript, childStepsRunScript) = runScript(step, param, uploadedFiles, wfName, tenantId);
                            return LoadStep(childStepsRunScript, resultRunScript, wfName, tenantId, uploadedFiles);

                        case WF_Type.CallDLLxJsonOnly:
                            var (resultDll, childStepsDll) = runCallDLL(step, param, tenantId);
                            return LoadStep(childStepsDll, resultDll, wfName, tenantId, uploadedFiles);

                        case WF_Type.CallAPIxJsonOnly:
                            var (resultCallAPI, childStepsAPI) = runCallAPI(step, param, wfName, tenantId);
                            return LoadStep(childStepsAPI, resultCallAPI, wfName, tenantId, uploadedFiles);

                        case WF_Type.DumpResponse:
                            var (resultDummy, childStepsDump) = runDumpResponse(step, param, tenantId);
                            return LoadStep(childStepsDump, resultDummy, wfName, tenantId, uploadedFiles);

                        case WF_Type.Condition:
                            var (resultCondition, childStepsCondition) = runCondition(step, param, tenantId);
                            return LoadStep(condition(step.Config ?? "", step, childStepsCondition, param, step.Param, tenantId), resultCondition, wfName, tenantId, uploadedFiles);

                        case WF_Type.SQLExecute:
                            var (resultSql, childStepsSQL) = runSQLExecute(step, param, wfName, tenantId);
                            return LoadStep(childStepsSQL, resultSql, wfName, tenantId, uploadedFiles);

                        case WF_Type.Delay:
                            var (resultDelay, childStepsDelay) = runDelay(step, param, tenantId);
                            return LoadStep(childStepsDelay, resultDelay, wfName, tenantId, uploadedFiles);

                        case WF_Type.RecallWF:
                            var (resultRecallWf, childStepsRecall) = runRecallWF(step, param, wfName, tenantId);
                            return LoadStep(childStepsRecall, resultRecallWf, wfName, tenantId, uploadedFiles);

                        case WF_Type.ReadArray:
                            var (resultReadArray, childStepsReadArray) = runReadArray(step, param, wfName, tenantId);
                            return LoadStep(childStepsReadArray, resultReadArray, wfName, tenantId, uploadedFiles);

                        case WF_Type.SendMail:
                            var (resultSendMail, childStepsEmail) = runSendEmail(step, param, tenantId);
                            return LoadStep(childStepsEmail, resultSendMail, wfName, tenantId, uploadedFiles);

                        case WF_Type.BuildListObject:
                            var (resultBuildListObj, childStepsBuildList) = runBuildListObject(step, param, tenantId);
                            return LoadStep(childStepsBuildList, resultBuildListObj, wfName, tenantId, uploadedFiles);

                        case WF_Type.Formatting:
                            return runFormating(step, param, wfName, tenantId);

                        case WF_Type.FindInList:
                            var (responseFindInList, childStepsFindInList) = runFindInList(step, param, tenantId);
                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = param,
                                ResponseBody = responseFindInList,
                                StepID = step.Id,
                                StepName = step.Description,
                                ErrorMessage = string.Empty,
                                WorkflowId = step.WfId
                            });
                            return LoadStep(childStepsFindInList, responseFindInList, wfName, tenantId, uploadedFiles);

                        case WF_Type.InformixExecute:
                            var (resultInformixExecute, childStepsInformixExecute) = runInformixExecute(step, param, tenantId);
                            return LoadStep(childStepsInformixExecute, resultInformixExecute, wfName, tenantId, uploadedFiles);

                        case WF_Type.HttpStatusResponse:
                            var (resultHttpStatusRes, childStepsHttpStatusRes) = runHttpStatusResponse(step, param, tenantId, ref httpResponse);
                            return LoadStep(childStepsHttpStatusRes, resultHttpStatusRes, wfName, tenantId, uploadedFiles);

                        default:
                            // End flow
                            saveDebug(new ModelXWorkflowDebug
                            {
                                RequestBody = param,
                                ResponseBody = param,
                                StepID = step.Id,
                                StepName = step.Description,
                                ErrorMessage = string.Empty,
                                WorkflowId = step.WfId
                            });
                            return this._modelControl.BindingData(step.Response, param, tenantId);
                    }
                }
                catch (Exception ex)
                {
                    saveDebug(new ModelXWorkflowDebug
                    {
                        RequestBody = param,
                        ResponseBody = "",
                        StepID = wFstep.FirstOrDefault()?.Id ?? "Error",
                        StepName = wFstep.FirstOrDefault()?.Description ?? "Error",
                        ErrorMessage = ex.Message,
                        WorkflowId = step.WfId
                    });

                    Log.Error(exception: ex, messageTemplate: "Error Execute Step", propertyValue: step.Id);

                    // Kiểm tra có exception steps không
                    var exceptionSteps = step.ChildSteps?.Where(ptr => ptr.IsExceptionStep == true).ToList();

                    if (exceptionSteps != null && exceptionSteps.Any())
                    {
                        // CÓ exception steps → Chạy exception steps, GIỮ NGUYÊN param
                        return LoadStep(exceptionSteps, param, wfName, tenantId, uploadedFiles);
                    }

                    // KHÔNG có exception steps → Chạy childSteps thông thường
                    var normalChildSteps = step.ChildSteps?.Where(ptr => ptr.IsExceptionStep != true).ToList();

                    if (normalChildSteps != null && normalChildSteps.Any())
                    {
                        // GIỮ NGUYÊN param, KHÔNG dùng BindingDataDump
                        return LoadStep(normalChildSteps, param, wfName, tenantId, uploadedFiles);
                    }

                    // Không có child steps nào → Return param gốc
                    return param;
                }
            }

            return param;
        }


        private TConfig GetConfig<TConfig>(WFStep step) where TConfig : FormattingConfig
        {
            TConfig formattingConfig = JsonConvert.DeserializeObject<TConfig>(step.Config);

            if (formattingConfig == null)
            {
                throw new IboxLog(string.Format("Can not found Config in step {0}", step.Id), "AppLogs");
            }

            return formattingConfig;
        }

        private void saveCahe(string stepID, WFCache response)
        {
            try
            {
                if (caches.Any(ptr => ptr.Key == stepID))
                {
                    caches[stepID] = response;
                }
                else
                {
                    caches.Add(stepID, response);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when save cache in Step");
            }
        }

        private void saveDebug(ModelXWorkflowDebug debug)
        {
            if (debugFlag && debug != null)
            {
                this.debug.Add(debug);
                try
                {
                    Log.Debug("Debug Work flow step {0} - {1}:\n {@debug:lj}", debug.StepID, debug.StepName, debug);
                }
                catch (Exception ex)
                {
                    Log.Error("Error in debuging: {@ex}", ex);
                }
            }
        }

        private WFCache? getCache(string stepID)
        {
            try
            {
                if (caches.Any(ptr => ptr.Key == stepID))
                {
                    return caches.FirstOrDefault(ptr => ptr.Key == stepID).Value;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when save cache in Step");
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_restAPI != null)
                    {
                        _restAPI.Dispose();
                    }

                    if (_connection != null)
                    {
                        _connection.Dispose();
                    }

                    foreach (var item in caches)
                    {
                        item.Value.Dispose();
                    }

                    if (caches.Count > 0)
                    {
                        caches.Clear();
                    }
                }

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}