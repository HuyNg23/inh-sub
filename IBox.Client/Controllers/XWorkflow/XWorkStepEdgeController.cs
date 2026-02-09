using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.XWorkflow
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class XWorkStepEdgeController : ActionController<WF_Step_Edge, TenantContext>
    {
        public XWorkStepEdgeController(IBContext<TenantContext> mainDB, IEncryption encryption) : base(mainDB, encryption)
        {
        }

        [IBoxActionPermission("integration-config-workflow-view", "integration-config-workflow-manage")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-workflow-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return base.Create(req);
        }

        [IBoxActionPermission("integration-config-workflow-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        [IBoxActionPermission("integration-config-workflow-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            return base.Update(req);
        }
    }
}