using IBox.Common.Objects;
using IBox.Workflow.Model;

namespace IBox.Client.Business.Model
{
    public class BWorkflow : BBaseBusiness<BWorkflow>
    {
        public string WorkflowId { get; set; } = string.Empty;

        public WFDefine GetByID()
        {
            if (string.IsNullOrEmpty(WorkflowId))
            {
                throw new IboxLog("workflowId is required", "AppLogs");
            }

            if (this.tenantContext == null)
            {
                throw new IboxLog("tenant context not valiable", "AppLogs");
            }

            return this.WfControl.SetTenantContext(this.tenantContext).GetByIDInDB(WorkflowId);
        }

        public WFDefine GetWFDeployByID(string tenantId)
        {
            if (string.IsNullOrEmpty(WorkflowId))
            {
                throw new IboxLog("workflowId is required",tenantId);
            }

            if (this.tenantContext == null)
            {
                throw new IboxLog("tenant context not valiable", tenantId);
            }

            return this.WfControl.SetTenantContext(this.tenantContext).GetWFDeployByID(WorkflowId, tenantId);
        }
    }
}