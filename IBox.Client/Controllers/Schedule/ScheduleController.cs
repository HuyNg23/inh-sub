using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.ScheduleHistory.Execution;
using IBox.Security;
using IBox.Workflow.Execution;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IBox.Client.Controllers.Schedule
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class ScheduleController : ActionController<S_Schedule, TenantContext>
    {
        private readonly IExecuteImplementationHistories _implementationHistories;

        public ScheduleController(IBContext<TenantContext> context, IWorkflowControl wfcontrol, IEncryption encryption, IExecuteImplementationHistories implementationHistories) : base(context, encryption)
        {
            _implementationHistories = implementationHistories;
        }

        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return base.Create(req);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            var headers = HttpContext.Request.Headers;
            var author = headers.Authorization.ToString();
            S_Schedule s_Schedule = JsonConvert.DeserializeObject<S_Schedule>(req.Body.ToString());
            _implementationHistories.CallAPIDeleteJobSchedule(s_Schedule, author);
            return base.Delete(req);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            return base.Update(req);
        }
    }
}