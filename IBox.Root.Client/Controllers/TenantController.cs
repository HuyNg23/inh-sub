using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class TenantController : ActionController<T_Tenant, RootContext>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBContext<RootContext> _rootContext;

        public TenantController(IBContext<RootContext> context, IServiceProvider serviceProvider, IBContext<RootContext> rootContext) : base(context)
        {
            _serviceProvider = serviceProvider;
            _rootContext = rootContext;
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                BTenantEnvironment.BTenantEnvironmentBuilder tenant = JsonConvert.DeserializeObject(req.Body.ToString(), typeof(BTenantEnvironment.BTenantEnvironmentBuilder));
                tenant.Init(_serviceProvider).ConfigTenantDatabase(tenant);
                return "Success";
            });
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var tenantModel = CheckValid(req);
                var data = _rootContext.Context.T_Tenants.FirstOrDefault(ptr => ptr.Id == tenantModel.Id && !ptr.IsDelete);

                if (data == null)
                {
                    throw new IboxLog($"Does not exist Tenant: {tenantModel.Id}", tenantModel.Id);
                }

                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Xóa tenant
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                BAccount.DeleteAccount reqDelete = JsonConvert.DeserializeObject(req.Body.ToString(), typeof(BAccount.DeleteAccount));
                return reqDelete.Init(_serviceProvider).CheckIsValidRootPassword(reqDelete);
            });
        }
    }
}