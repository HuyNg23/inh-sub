using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Database
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class GroupDLLController : ActionController<GroupDynamicLinkedLibrary, TenantContext>
    {
        public GroupDLLController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        [IBoxActionPermission("integration-config-library-manage", "integration-config-library-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-library-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var GroupDynamicModel = CheckValid(req);

                if (string.IsNullOrEmpty(GroupDynamicModel.Name?.Trim()))
                {
                    throw new IboxLog("Group name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.GroupDynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Name == GroupDynamicModel.Name && !ptr.IsDelete);

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

        [IBoxActionPermission("integration-config-library-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var GroupDynamicModel = CheckValid(req);

                if (string.IsNullOrEmpty(GroupDynamicModel.Name?.Trim()))
                {
                    throw new IboxLog("Group name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.GroupDynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Name == GroupDynamicModel.Name && ptr.Id != GroupDynamicModel.Id && !ptr.IsDelete);

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

        [HttpPost("GetAllGroupDll")]
        [IBoxActionPermission("integration-config-library-manage", "integration-config-library-view")]
        public ResponseForm<List<ResponseDll>> GetAllGroupDll(RequestForm<RequestDll> req)
        {
            try
            {
                var query = from a in base.IBContext.Context.GroupDynamicLinkedLibraries
                            join b in base.IBContext.Context.DynamicLinkedLibraries on a.Id equals b.GroupId into leftjDynamiclink
                            from b in leftjDynamiclink.DefaultIfEmpty()
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




                var result = filteredQuery
                            .GroupBy(ti => new
                            {
                                ti.a.Name,
                                ti.a.Id,
                                ti.a.CreatedDate,
                                ti.a.IsDelete,
                                ti.a.ModificationDate
                            }).Select(g => new ResponseDll
                            {
                                name = g.Key.Name,
                                id = g.Key.Id,
                                createdDate = g.Key.CreatedDate,
                                isDelete = g.Key.IsDelete,
                                modificationDate = g.Key.ModificationDate,
                                list = g.Where(ti => ti.b != null).Select(ti => new BDynamicLinkedLibrary
                                {
                                    Id = ti.b?.Id,
                                    Name = ti.b?.Name,
                                    Config = ti.b?.Config,
                                    FunctionList = ti.b?.FunctionList,
                                    FileName = ti.b?.Link.Split(Path.DirectorySeparatorChar).LastOrDefault(),
                                    CreatedDate = ti.b?.CreatedDate,
                                    GroupId = ti.b?.GroupId
                                }).OrderByDescending(rds => rds.CreatedDate).ToList()
                            }).OrderByDescending(rds => rds.createdDate);


                return new ResponseForm<List<ResponseDll>>(() => result.ToList());
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<ResponseDll>>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-library-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }
    }
}