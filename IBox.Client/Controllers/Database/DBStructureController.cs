using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Database
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class DBStructureController : ActionController<DBStructure, TenantContext>
    {
        public DBStructureController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
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

        /// <summary>
        /// Tạo nhóm object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var DBStructureModel = CheckValid(req);

                if (string.IsNullOrEmpty(DBStructureModel.Name?.Trim()))
                {
                    throw new IboxLog("Group name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.DBStructures.FirstOrDefault(ptr => ptr.Name == DBStructureModel.Name && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Group name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Cập nhật nhóm object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-model-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var DBStructureModel = CheckValid(req);

                if (string.IsNullOrEmpty(DBStructureModel.Name?.Trim()))
                {
                    throw new IboxLog("Group name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.DBStructures.FirstOrDefault(ptr => ptr.Name == DBStructureModel.Name && ptr.Id != DBStructureModel.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Group name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Lấy danh sách nhóm object
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("GetAllGroupObject")]
        [IBoxActionPermission("integration-config-model-manage", "integration-config-model-view")]
        public ResponseForm<List<ResponseDataStructure>> View(RequestForm<RequestDataStructure> req)
        {
            try
            {
                var query = from a in base.IBContext.Context.DBStructures
                            join b in base.IBContext.Context.Objs on a.Id equals b.DatabaseID into leftjObjs
                            from b in leftjObjs.DefaultIfEmpty()
                            where a.IsDelete != true && b.IsDelete != true
                            select new
                            {
                                a,
                                b
                            };

                var filteredQuery = query.AsEnumerable().Where(ti =>
                ti.b == null ? (string.IsNullOrEmpty(req.Body.value)
                            || ti.a.Name.Contains(req.Body.value)
                            || ti.a.Id.ToString().Contains(req.Body.value)) :
                                    (string.IsNullOrEmpty(req.Body.value)
                            || ti.a.Name.Contains(req.Body.value)
                            || ti.a.Id.ToString().Contains(req.Body.value)
                            || ti.b.Name.Contains(req.Body.value)
                            || ti.b.Id.ToString().Contains(req.Body.value)
                            ));



#pragma warning disable CS8601 // Possible null reference assignment.
                var result = filteredQuery
                            .GroupBy(ti => new
                            {
                                ti.a.Name,
                                ti.a.Id,
                                ti.a.CreatedDate,
                                ti.a.IsDelete,
                                ti.a.ModificationDate
                            }).Select(g => new ResponseDataStructure
                            {
                                name = g.Key.Name,
                                id = g.Key.Id,
                                createdDate = g.Key.CreatedDate,
                                isDelete = g.Key.IsDelete,
                                modificationDate = g.Key.ModificationDate,
                                list = g.Where(ti => ti.b != null).Select(ti => new Obj
                                {
                                    Name = ti.b?.Name,
                                    DatabaseID = ti.b?.DatabaseID,
                                    Id = ti.b?.Id,
                                    CreatedDate = ti.b?.CreatedDate,
                                    IsDelete = ti.b == null ? false : ti.b.IsDelete,
                                    ModificationDate = ti.b?.ModificationDate
                                }).OrderByDescending(rds => rds.CreatedDate).ToList()
                            }).OrderByDescending(rds => rds.createdDate);
#pragma warning restore CS8601 // Possible null reference assignment.

                return new ResponseForm<List<ResponseDataStructure>>(() => result.ToList());
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<ResponseDataStructure>>(() => throw ex);
            }
        }
    }
}