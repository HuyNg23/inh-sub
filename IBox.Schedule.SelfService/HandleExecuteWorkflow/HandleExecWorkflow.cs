using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using IBox.Workflow.Execution;
using Quartz;
using Serilog;

namespace IBox.Schedule.SelfService.HandleExecuteWorkflow
{
    public class HandleExecWorkflow : IHandleExecWorkflow
    {
        private readonly IExecuteWF _executeWF;
        private readonly IWorkflowControl _workflowControl;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public HandleExecWorkflow(IExecuteWF executeWF, IWorkflowControl workflowControl, IConfiguration configuration, IEncryption encryption)
        {
            _executeWF = executeWF;
            _workflowControl = workflowControl;
            _configuration = configuration;
            _encryption = encryption;
        }

        public void ExecWorkflow(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var wfid = dataMap.GetString("wfid") ?? string.Empty;
            var planId = dataMap.GetString("planId") ?? string.Empty;
            var scheduleId = dataMap.GetString("scheduleId") ?? string.Empty;
            var tenantId = dataMap.GetString("tenantId") ?? string.Empty;
            var typeSchedule = dataMap.GetString("typeSchedule") ?? string.Empty;
            var jobKey = dataMap.GetString("jobKey") ?? string.Empty;
            var isDeploy = dataMap.GetString("isDeploy") ?? string.Empty;
            var thisSite = this._configuration.Config.Value.ThisSite ?? string.Empty;
            var keyWF = Guid.NewGuid().ToString();

            try
            {
                Log.Information($"JobKey Execute: {jobKey}");

                if (string.IsNullOrEmpty(wfid))
                {
                    CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Fail, thisSite, typeSchedule, keyWF, "Wfid input IsNullOrEmpty.");

                    Log.Error($"ExecuteJob fail because wfid is null or empty.");

                    return;
                }

                var checkWfExist = CheckWfExist(tenantId, wfid);

                if (!checkWfExist)
                {
                    CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Fail, thisSite, typeSchedule, keyWF, $"Not found wfid: {wfid}");

                    Log.Error($"ExecuteJob fail because not found wfid: {wfid}");

                    return;
                }

                var checkWorkflowDelete = CheckWorkflowDelete(tenantId, wfid);

                if (checkWorkflowDelete)
                {
                    CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Fail, thisSite, typeSchedule, keyWF, $"Workflow with id: {wfid} has been deleted.");

                    Log.Error($"ExecuteJob fail because wfid: {wfid} has been deleted.");

                    return;
                }

                CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Running, thisSite, typeSchedule, keyWF, "");

                ExecuteWorkflow(tenantId, wfid, isDeploy);

                var checkExistPlanHistory = CheckExistImplementationHistory(tenantId, keyWF);

                if (!string.IsNullOrEmpty(checkExistPlanHistory))
                {
                    UpdateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Complete, thisSite, typeSchedule, keyWF, "");
                }
                else
                {
                    CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Complete, thisSite, typeSchedule, keyWF, "");
                }

            }
            catch (Exception ex)
            {
                var checkExistPlanHistory = CheckExistImplementationHistory(tenantId, keyWF);

                if (!string.IsNullOrEmpty(checkExistPlanHistory))
                {
                    UpdateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Fail, thisSite, typeSchedule, keyWF, ex.Message);
                }
                else
                {
                    CreateImplementationHistory(tenantId, planId, scheduleId, StatusPlan.Fail, thisSite, typeSchedule, keyWF, ex.Message);
                }

                Log.Error("ExecuteJob_Exception: " + ex.Message);
            }
        }

        private bool CheckWfExist(string tenantId, string wfid)
        {
            var rootContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = rootContext.GetTenantContext(tenantId).Context;

            var checkWfExist = tenantContext.WF_Defines.Where(ptr => ptr.Id == wfid).FirstOrDefault();

            tenantContext.Context.Dispose();

            if (checkWfExist != null)
            {
                return true;
            }

            return false;
        }

        private bool CheckWorkflowDelete(string tenantId, string wfid)
        {
            var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var checkWfExist = tenantContext.WF_Defines.Where(ptr => ptr.Id == wfid).FirstOrDefault();

            tenantContext.Context.Dispose();

            if (checkWfExist != null && checkWfExist.IsDelete)
            {
                return true;
            }

            return false;
        }

        private string CheckExistImplementationHistory(string tenantId, string keyWF)
        {
            var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var DataImplementationHistorys = tenantContext.S_ImplementationHistorys.FirstOrDefault(ptr => ptr.KeyExecuteRunWorkFlow == keyWF);

            if (DataImplementationHistorys != null)
            {
                return DataImplementationHistorys.Id;
            }

            tenantContext.Context.Dispose();

            return string.Empty;
        }

        private void CreateImplementationHistory(string tenantId, string planId, string scheduleId, StatusPlan statusPlan, string thisSite, string typeSchedule, string keyWF, string message)
        {
            var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            EntityAction.TryCreate<S_ImplementationHistory>(null, tenantContext, new S_ImplementationHistory()
            {
                ImplementationPlanID = planId,
                ScheduleID = scheduleId,
                StatusPlan = statusPlan,
                Reason = message ?? string.Empty,
                Site = thisSite,
                Type = (TypeScheduleBase)Enum.Parse(typeof(TypeScheduleBase), typeSchedule),
                KeyExecuteRunWorkFlow = keyWF,
            });

            tenantContext.Context.Dispose();
        }

        private void UpdateImplementationHistory(string tenantId, string planId, string scheduleId, StatusPlan statusPlan, string thisSite, string typeSchedule, string keyWF, string message)
        {
            var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var dataImplementationHistorys = tenantContext.S_ImplementationHistorys.FirstOrDefault(ptr => ptr.KeyExecuteRunWorkFlow == keyWF);

            if (dataImplementationHistorys == null)
            {
                return;
            }

            dataImplementationHistorys.TryUpdate(tenantContext, new S_ImplementationHistory()
            {
                Id = dataImplementationHistorys.Id,
                ImplementationPlanID = planId,
                ScheduleID = scheduleId,
                StatusPlan = statusPlan,
                Reason = message ?? string.Empty,
                Site = thisSite,
                Type = (TypeScheduleBase)Enum.Parse(typeof(TypeScheduleBase), typeSchedule),
                KeyExecuteRunWorkFlow = keyWF,
            });

            tenantContext.Context.Dispose();
        }

        private void ExecuteWorkflow(string tenantId, string wfid, string isDeploy)
        {
            var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            this._executeWF.SetTenantContext(tenantContext);

            if (isDeploy == "True")
            {
                this._executeWF.Execute(wfid, "{}", true);
            }
            else
            {
                this._executeWF.Execute(wfid, "{}", this._workflowControl.UpdateDateToUpdate(wfid));
            }

            tenantContext.Context.Dispose();
        }
    }
}
