using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.XObject
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class XObjectController : ActionController<Obj, TenantContext>
    {
        public XObjectController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        /// <summary>
        /// Tạo object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Object name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.Objs.FirstOrDefault(ptr => ptr.Name == objModel.Name && ptr.DatabaseID == objModel.DatabaseID && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Object name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Cập nhật object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Object name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.Objs.FirstOrDefault(ptr => ptr.Name == objModel.Name && ptr.Id != objModel.Id && ptr.DatabaseID == objModel.DatabaseID && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Object name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
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