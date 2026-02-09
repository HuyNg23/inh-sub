using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx.Execution;
using IBox.DLEx.Implementation;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Schedule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class WorkflowController : ControllerBase
    {
        private readonly IExecuteWF _executeWF;
        private readonly IBContext<TenantContext> _tenantContext;

        public WorkflowController(IExecuteWF executeDLL, IBContext<TenantContext> tenantContext)
        {
            _executeWF = executeDLL;
            _tenantContext = tenantContext;
        }

        [HttpPost("ViewWorkflowDeploy")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view")]
        public ResponseForm<dynamic> ViewWorkflowDeploy()
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).ViewWFDeploy(this.Request, ViewDeployServiceType.Schedule));
        }

        [HttpPost("ViewWorkflowDeployHA")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-view")]
        public ResponseForm<dynamic> ViewWorkflowDeployHA()
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).ViewWFDeploy());
        }

        [HttpPost("Deploy/{id}")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-deploy")]
        public ResponseForm<dynamic> DeployWF(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).Deploy(id, this.Request, ViewDeployServiceType.Schedule));
        }

        [HttpPost("DeployHA/{id}")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-deploy")]
        public ResponseForm<dynamic> DeployWFHA(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).Deploy(id, tenantID));
        }

        [HttpPost("Recovery/{id}")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-unlock")]
        public ResponseForm<dynamic> RecoveryWF(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).Recovery(id, this.Request, ViewDeployServiceType.Schedule));
        }

        [HttpPost("RecoveryHA/{id}")]
        [IBoxActionPermission("integration-config-workflow-manage", "integration-config-workflow-unlock")]
        public ResponseForm<dynamic> RecoveryWFHA(string id)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _executeWF.SetTenantContext(_tenantContext, tenantID).Recovery(id));
        }
    }
}