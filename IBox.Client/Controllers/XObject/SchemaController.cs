using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.XObject
{
    [Route("api/XObject/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class SchemaController : ActionController<Obj_Schema, TenantContext>
    {
        public SchemaController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
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

        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        /// <summary>
        /// Lấy danh sách trường trong object
        /// </summary>
        /// <param name="req"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [HttpPost("GetSchemabyObjectID/{id}")]
        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public ResponseForm<dynamic> GetSchemaByObjectID(RequestForm<dynamic> req, string id)
        {
            return new ResponseForm<dynamic>(() =>
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new IboxLog("Not found detail with id", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var record = IBContext.Context.Obj_Schemas.Where(ptr => ptr.ObjID == id && !ptr.IsDelete).OrderBy(ptr => ptr.CreatedDate).ToList();
                var recordDeleted = IBContext.Context.Obj_Schemas.Where(ptr => ptr.ObjID == id && ptr.IsDelete).OrderBy(ptr => ptr.CreatedDate).ToList();
                return new
                {
                    avaible = record,
                    deleted = recordDeleted
                };
            });
        }

        /// <summary>
        /// Tạo trường trong object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var Obj_SchemasModel = CheckValid(req);

                if (string.IsNullOrEmpty(Obj_SchemasModel.FieldName?.Trim()))
                {
                    throw new IboxLog("Field Name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.Obj_Schemas.FirstOrDefault(ptr => ptr.FieldName == Obj_SchemasModel.FieldName && ptr.ObjID == Obj_SchemasModel.ObjID && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Field Name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Cập nhật trường trong object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var objSchemasModel = CheckValid(req);

                if (string.IsNullOrEmpty(objSchemasModel.FieldName?.Trim()))
                {
                    throw new IboxLog("Field Name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.Obj_Schemas.FirstOrDefault(ptr => ptr.FieldName == objSchemasModel.FieldName && ptr.Id != objSchemasModel.Id && ptr.ObjID == objSchemasModel.ObjID && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Field Name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
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