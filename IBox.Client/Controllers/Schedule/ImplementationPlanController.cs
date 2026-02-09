using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Schedule
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImplementationPlanController : ActionController<S_ImplementationHistory, TenantContext>
    {
        public ImplementationPlanController(IBContext<TenantContext> Context, IEncryption encryption) : base(Context, encryption)
        {
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
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

        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return new ResponseForm<List<dynamic>>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }
    }
}