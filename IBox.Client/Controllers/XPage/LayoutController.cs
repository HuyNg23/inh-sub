using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Mvc;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Common.Objects;
using IBox.Security;

namespace IBox.Client.Controllers.XPage
{
    [Route("api/[controller]")]
    [ApiController]
    public class LayoutController : ActionController<P_Layout, TenantContext>
    {
        public LayoutController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
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
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Layout name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.P_Layouts.FirstOrDefault(ptr => ptr.Name == objModel.Name && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Layout name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

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
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Layout name can't null or empty.",base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.P_Layouts.FirstOrDefault(ptr => ptr.Name == objModel.Name && ptr.Id != objModel.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Layout name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }
    }
}
