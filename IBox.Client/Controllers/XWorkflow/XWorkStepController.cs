using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
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
    public class XWorkStepController : ActionController<WF_Step, TenantContext>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRestAPI restAPI;

        public XWorkStepController(IBContext<TenantContext> mainContext, IServiceProvider serviceProvider, IEncryption encryption, IRestAPI restAPI) : base(mainContext, encryption)
        {
            _serviceProvider = serviceProvider;
            this.restAPI = restAPI;
        }

        [IBoxActionPermission("integration-config-workflow-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var reWF_Step = CheckValid(req);

                if (reWF_Step.WFid == null)
                {
                    throw new IboxLog("WFid Step is null", "AppLogs");
                }

                this.restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = IBContext.Context.TenantInfo.Id,
                    MailTitle = "[Warning] Create Step Note Mới!",
                    MailBody = $"Note Mới",
                    WFID = reWF_Step.WFid,
                    typeWarning = TypeWarning.CreateStepWF
                });

                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-workflow-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var reWF_Step = CheckValid(req);

                if (reWF_Step.Id == null)
                {
                    throw new IboxLog("ID Step is null", "AppLogs");
                }

                var wF_Step = IBContext.Context.WF_Steps.FirstOrDefault(x => x.Id == reWF_Step.Id && !x.IsDelete);

                if (wF_Step == null)
                {
                    throw new IboxLog("WF_Step is null", "AppLogs");
                }

                this.restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = IBContext.Context.TenantInfo.Id,
                    MailTitle = "[Warning] Update Step!",
                    MailBody = $"{wF_Step.Name}",
                    WFID = wF_Step.WFid,
                    typeWarning = TypeWarning.UpdateStepWF
                });

                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("DeleteStep")]
        [IBoxActionPermission("integration-config-workflow-manage")]
        public ResponseForm<dynamic> DeleteStep(RequestForm<BWorkstep> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                req.Body.SetTenantContext(IBContext).Init(_serviceProvider).RemoveStep();
            });
        }
    }
}