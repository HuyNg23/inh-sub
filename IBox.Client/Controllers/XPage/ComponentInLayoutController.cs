using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Mvc;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Security;

namespace IBox.Client.Controllers.XPage
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComponentInLayoutController : ActionController<P_ComponentInLayout, TenantContext>
    {
        public ComponentInLayoutController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        /// <summary>
        /// Tạo component
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Cập nhật component
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }
    }
}
