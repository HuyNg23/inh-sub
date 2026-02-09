using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Workflow.Model;
using Newtonsoft.Json;
using Serilog;

namespace IBox.Workflow.Execution
{
    public class WorkflowControl : ATenantContext<WorkflowControl>, IWorkflowControl
    {
        private readonly IModelControl _modelControl;
        private readonly IRestAPI _restAPI;
        private readonly IEncryption _encryption;
        private readonly IConfiguration _configuration;

        public WorkflowControl(IModelControl control, IRestAPI restAPI, IEncryption encryption, IConfiguration configuration)
        {
            _modelControl = control;
            _restAPI = restAPI;
            _encryption = encryption;
            _configuration = configuration;
        }

        protected override void ImplementationDIInLocalObject()
        {
            if (this.tenantContext == null)
            {
                throw new IboxLog("Tenant context have not aviable", "AppLogs");
            }

            _modelControl.SetTenantContext(this.tenantContext);
        }

        public override WorkflowControl SetTenantContext(IBContext<TenantContext> tenantContext)
        {
            base.SetTenantContext(tenantContext);
            this.ImplementationDIInLocalObject();
            return this;
        }

        public override WorkflowControl SetTenantContext(IBContext<TenantContext> tenantContext, string tenantID)
        {
            base.SetTenantContext(tenantContext, tenantID);
            this.ImplementationDIInLocalObject();
            return this;
        }

        /// <summary>
        /// Deploy lên local
        /// </summary>
        /// <param name="wfid"></param>
        /// <exception cref="Exception"></exception>
        public void Deploy(string wfid, string tenantId)
        {
            try
            {
                if (wfid == string.Empty)
                {
                    throw new IboxLog("Workflow id is null or empty", tenantId);
                }

                var rootContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = rootContext.GetTenantContext(tenantId).Context;

               
                var wf = tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == wfid);
                if (wf == null)
                {
                    //_restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                    //{
                    //    Id = this.tenantContext.Context.TenantInfo.Id,
                    //    MailTitle = "[Warning] API expose từ IBOX không thể tìm thấy",
                    //    WFID = wfid,
                    //    typeWarning = TypeWarning.APINotFound
                    //});
                    throw new IboxLog($"not found any Workflow has id: {wfid}", tenantId);
                }

                if (wf.IsLock == true)
                {
                    throw new IboxLog(string.Format("wf id {0} is locked", wf.Id), tenantId);
                }

                wf.IsDeploy = true;

                wf.TryUpdate(tenantContext.Context, wf);

                var allSteps = tenantContext.Context.WF_Steps
                    .Where(ptr => ptr.WFid == wf.Id && !ptr.IsDelete)
                    .ToList();

                var dllIds = allSteps.Select(s => s.DllID).Distinct().ToList();
                var dlls = tenantContext.Context.DynamicLinkedLibraries
                    .Where(dll => dllIds.Contains(dll.Id))
                    .ToDictionary(dll => dll.Id);

                var stepGroups = allSteps
                    .Where(s => !string.IsNullOrEmpty(s.ParentStep))
                    .GroupBy(s => s.ParentStep)
                    .ToDictionary(g => g.Key, g => g.ToList());

                List<WFStep> BuildSteps(string parentId)
                {
                    if (!stepGroups.ContainsKey(parentId))
                        return new List<WFStep>();

                    return stepGroups[parentId].Select(ptr =>
                    {
                        var step = new WFStep
                        {
                            Id = ptr.Id,
                            Debug = ptr.Debug,
                            IsDelete = ptr.IsDelete,
                            Description = ptr.Description,
                            Type = ptr.Type,
                            WfId = ptr.WFid,
                            Param = ptr.Param,
                            Response = ptr.Response,
                            FunctionName = ptr.FunctionName,
                            SaveResponseToCache = ptr.SaveResponseToCache,
                            DllID = ptr.DllID,
                            Config = ptr.Config,
                            IsExceptionStep = ptr?.IsExceptionStep ?? false,
                            DynamicLinkedLibrary = dlls.ContainsKey(ptr.DllID) ? dlls[ptr.DllID] : null,
                            ChildSteps = BuildSteps(ptr.Id)
                        };

                        return step;
                    }).ToList();
                }

                var wfsteps = allSteps
                    .Where(ptr => ptr.Type == Database.Tenant.Tables.WF_Type.Start && !ptr.IsDelete)
                    .Select(ptr =>
                    {
                        var step = new WFStep
                        {
                            Id = ptr.Id,
                            Debug = ptr.Debug,
                            IsDelete = ptr.IsDelete,
                            Description = ptr.Description,
                            Type = ptr.Type,
                            WfId = ptr.WFid,
                            Param = ptr.Param,
                            Response = ptr.Response,
                            FunctionName = ptr.FunctionName,
                            SaveResponseToCache = ptr.SaveResponseToCache,
                            DllID = ptr.DllID,
                            Config = ptr.Config,
                            IsExceptionStep = ptr?.IsExceptionStep ?? false,
                            DynamicLinkedLibrary = dlls.ContainsKey(ptr.DllID) ? dlls[ptr.DllID] : null,
                            ChildSteps = BuildSteps(ptr.Id)
                        };
                        return step;
                    }).ToList();

                wfsteps.ForEach(step =>
                {
                    BuildModelRecursive(step, tenantId, tenantContext);
                });

                wfsteps.ForEach(ptr =>
                {
                    if (!string.IsNullOrEmpty(ptr.Param) && !string.IsNullOrEmpty(ptr.Response))
                    {
                        this._modelControl.BuildModel(ptr.Param, tenantId, tenantContext);
                        this._modelControl.BuildModel(ptr.Response, tenantId, tenantContext);
                    }
                    else
                    {
                        //Log.Warning(string.Format("Param or response model not config in step {0}", ptr.Id));
                    }
                });

                var staticWF = ListTenantWF.TenantWFs.GetOrAdd(tenantId, _ => new StaticListWF());
                if (staticWF.WFs.TryGetValue(wf.Id, out var existingWf))
                {
                    existingWf.Dispose(); // Giải phóng nếu có
                    staticWF.WFs.TryRemove(wf.Id, out _);
                }

                staticWF.WFs.TryAdd(wf.Id, new Model.WFDefine()
                {
                    TenantId = tenantId,
                    Id = wf.Id,
                    Name = wf.Name ?? string.Empty,
                    Description = wf.Description ?? string.Empty,
                    WFstep = wfsteps,
                    AuthenType = wf.AuthenType ?? AuthorType.None,
                    UserName = wf.UserName ?? string.Empty,
                    Password = wf.Password ?? string.Empty,
                    DeployDate = DateTime.Now,
                });

                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }
        void BuildModelRecursive(WFStep step, string tenantId, IBContext<TenantContext> tenantContext)
        {
            if (!string.IsNullOrEmpty(step.Param) && !string.IsNullOrEmpty(step.Response))
            {
                this._modelControl.BuildModel(step.Param, tenantId, tenantContext);
                this._modelControl.BuildModel(step.Response, tenantId, tenantContext);
            }
            else
            {
            }

            if (step.ChildSteps != null && step.ChildSteps.Any())
            {
                foreach (var child in step.ChildSteps)
                    BuildModelRecursive(child, tenantId, tenantContext);
            }
        }
        /// <summary>
        /// Thu hồi trên local
        /// </summary>
        /// <param name="wfid"></param>
        /// <exception cref="Exception"></exception>
        public void Recovery(string wfid)
        {
            string tenantId = this.tenantContext.Context.TenantInfo.Id;

            try
            {
                if (wfid == string.Empty)
                {
                    throw new IboxLog("Workflow id is null or empty", tenantId);
                }
                var wf = this.tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == wfid);
                if (wf == null)
                {
                    throw new IboxLog(string.Format("not found any Workflow has id: {0}", wfid), tenantId);
                }

                wf.IsLock = true;
                wf.IsDeploy = false;
                wf.TryUpdate(this.tenantContext.Context, wf);

                var wfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
                Log.Information("Recovery WF start" + JsonConvert.SerializeObject(wfs.Value.WFs));
                if (wfs.Value.WFs.ContainsKey(wf.Id))
                {
                    wfs.Value.WFs.TryRemove(wf.Id, out _);
                    Log.Information("Recovery WF end" + JsonConvert.SerializeObject(wfs.Value.WFs));
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId ?? "AppLogs", ex);
            }
        }

        /// <summary>
        /// Danh sách wf đã được deploy trên local
        /// </summary>
        /// <returns></returns>
        public List<WFDefine> AllWFDeploy()
        {
            var temamtwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == this.tenantContext.Context.TenantInfo.Id);
            if (temamtwfs.Key == null)
            {
                return new List<WFDefine>();
            }

            return temamtwfs.Value.WFs.Values.Select(ptr => new WFDefine()
            {
                Id = ptr.Id,
                Name = ptr.Name,
                DeployDate = ptr.DeployDate,
            }).ToList();
        }

        /// <summary>
        /// get workflow trực tiếp từ Database
        /// </summary>
        /// <param name="wfid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public WFDefine GetByIDInDB(string wfid)
        {
            string tenantId = this.tenantContext.Context.TenantInfo.Id;

            if (wfid == string.Empty)
            {
                throw new IboxLog("Workflow id is null or empty", tenantId);
            }

            var wf = this.tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == wfid);

            if (wf == null)
            {
                throw new IboxLog(string.Format("not found any Workflow has id: {0}", wfid), tenantId);
            }

            //Fix tối ưu querry
            var allSteps = tenantContext.Context.WF_Steps
                .Where(ptr => ptr.WFid == wf.Id && !ptr.IsDelete)
                .ToList();

            var dllIds = allSteps.Select(s => s.DllID).Distinct().ToList();

            var dlls = tenantContext.Context.DynamicLinkedLibraries
                .Where(dll => dllIds.Contains(dll.Id))
                .ToDictionary(dll => dll.Id);

            var stepGroups = allSteps
                .GroupBy(s => s.ParentStep)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<WFStep> BuildSteps(string parentId)
            {
                if (!stepGroups.ContainsKey(parentId))
                    return new List<WFStep>();

                return stepGroups[parentId].Select(ptr =>
                {
                    var wfStep = new WFStep
                    {
                        Id = ptr.Id,
                        Debug = ptr.Debug,
                        IsDelete = ptr.IsDelete,
                        Description = ptr.Description,
                        DynamicLinkedLibrary = dlls.ContainsKey(ptr.DllID) ? dlls[ptr.DllID] : null,
                        Type = ptr.Type,
                        WfId = ptr.WFid,
                        PositionX = ptr.PositionX,
                        PositionY = ptr.PositionY,
                        Param = ptr.Param,
                        Response = ptr.Response,
                        SaveResponseToCache = ptr.SaveResponseToCache,
                        DllID = ptr.DllID,
                        FunctionName = ptr.FunctionName,
                        Config = ptr.Config,
                        IsExceptionStep = ptr.IsExceptionStep,
                        // Đệ quy lấy child step
                        ChildSteps = BuildSteps(ptr.Id)
                    };

                    // Gọi build model (nếu cần)
                    if (!string.IsNullOrEmpty(ptr.Param) && !string.IsNullOrEmpty(ptr.Response))
                    {
                        this._modelControl.BuildModel(ptr.Param, tenantId, tenantContext);
                        this._modelControl.BuildModel(ptr.Response, tenantId, tenantContext);
                    }
                    else
                    {
                        Log.Warning($"Param or response model not config in step {ptr.Id}");
                    }

                    return wfStep;
                }).ToList();
            }

            var wfsteps = BuildSteps(string.Empty);

            var edges = this.tenantContext.Context.WF_Step_Edges.Where(ptr => ptr.WFid == wf.Id && !ptr.IsDelete).ToList().Select(ptr => new WFEdge()
            {
                Id = ptr.Id,
                Source = ptr.Source,
                Target = ptr.Target,
                SourceHandle = ptr.SourceHandle,
            }).ToList();

            return new Model.WFDefine()
            {
                Id = wf.Id,
                Name = wf.Name ?? string.Empty,
                IsLock = wf.IsLock ?? false,
                Description = wf.Description ?? string.Empty,
                Edges = edges,
                WFstep = wfsteps,
                CreatedDate = wf.CreatedDate ?? DateTime.Now,
                ModificationDate = wf.ModificationDate ?? DateTime.Now,
                AuthenType = wf.AuthenType ?? AuthorType.None,
                UserName = wf.UserName ?? string.Empty,
                Password = wf.Password ?? string.Empty,
            };
        }

        /// <summary>
        /// Lấy workflow bằng id đã deploy lên local
        /// </summary>
        /// <param name="wfid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public WFDefine GetWFDeployByID(string wfid, string tenantId)
        {
            if (wfid == string.Empty)
            {
                //_restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                //{
                //    Id = this.tenantContext.Context.TenantInfo.Id,
                //    MailTitle = "[Warning] API expose từ IBOX không thể tìm thấy",
                //    WFID = wfid,
                //    typeWarning = TypeWarning.APINotFound
                //});
                throw new IboxLog(string.Format("workflow {0} can not found in deploy", wfid), tenantId);
            }

            var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
            if (tenantwfs.Key == null || tenantwfs.Value == null)
            {
                throw new IboxLog(string.Format("workflow {0} can not found in deploy", wfid), tenantId);
            }

            var wf = tenantwfs.Value.WFs.FirstOrDefault(ptr => ptr.Key == wfid);
            if (wf.Value == null)
            {
                Deploy(wfid, tenantId);
                wf = tenantwfs.Value.WFs.FirstOrDefault(ptr => ptr.Key == wfid);
            }

            if (wf.Value == null)
            {
                throw new IboxLog(string.Format("workflow {0} was not found after deploying", wfid), tenantId);
            }

            return wf.Value;
        }

        /// <summary>
        /// Kiểm tra workflow đã tồn tại trong danh sách ở local hay chưa, nếu chưa có thì sẽ đẩy thêm vào danh sách ở local
        /// </summary>
        /// <param name="wfid">ID của workflow</param>
        /// <returns></returns>
        public bool IsExist(string wfid, string tenantId)
        {
            var tenantwfs = ListTenantWF.TenantWFs.FirstOrDefault(ptr => ptr.Key == tenantId);
            if (tenantwfs.Key == null || tenantwfs.Value == null)
            {
                return false;
            }
            return tenantwfs.Value.WFs.ContainsKey(wfid);
        }

        public void DeployIfNotExist(string wfid, string tenantId)
        {
            if (!string.IsNullOrEmpty(wfid) && !IsExist(wfid, tenantId))
            {
                this.Deploy(wfid, tenantId);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _modelControl.Dispose();
                    _restAPI.Dispose();
                }

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}