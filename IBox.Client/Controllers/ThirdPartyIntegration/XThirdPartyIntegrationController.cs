using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.ThirdPartyIntegration
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    public class XThirdPartyIntegrationController : ActionController<ThirdPartyIntegrationConfiguration, TenantContext>
    {
        public XThirdPartyIntegrationController(IBContext<TenantContext> mainDB, IEncryption encryption) : base(mainDB, encryption)
        {
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            var listConfig = base.GetAll(req, pageNumber);
            if (listConfig.Data == null || listConfig.Data.Count == 0)
            {
                return listConfig;
            }

            listConfig.Data.ForEach(ptr => ((ThirdPartyIntegrationConfiguration)ptr).Config = "");

            return listConfig;
        }
    }
}