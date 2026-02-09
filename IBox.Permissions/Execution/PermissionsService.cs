using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Permissions.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace IBox.Permissions.Execution
{
    public class PermissionsService : IPermissionsService
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public PermissionsService(IConfiguration configuration, IEncryption encryption)
        {
            _configuration = configuration;
            _encryption = encryption;
        }

        public async Task<bool> CheckPermissionTenantAsync(string tenantId, string userId, string roleId, string controllerName, IEnumerable<string> actionNames)
        {
            try
            {
                if (roleId == "TENANT" || roleId == "ROOT")
                {
                    return true;
                }

                using (var rootContext = new RootContext(this._configuration, this._encryption))
                {
                    var userRoleExists = await rootContext.Context.U_Users
                        .AnyAsync(ptr => ptr.RoleID == roleId && ptr.Id == userId && !ptr.IsDelete);

                    if (!userRoleExists)
                    {
                        return false;
                    }

                    var rolePermissions = await rootContext.Context.U_Roles
                        .FirstOrDefaultAsync(ptr => ptr.Id == roleId && !ptr.IsDelete);

                    if (rolePermissions == null)
                    {
                        return false;
                    }

                    var permissionsKey = rolePermissions.PermissionsKey.ToLower();
                    var listActionKey = JsonConvert.DeserializeObject<List<string>>(permissionsKey);

                    if (listActionKey == null)
                    {
                        return false;
                    }

                    return actionNames.Any(actionName => listActionKey.Any(key => actionName.ToLower().Contains(key)));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex, tenantId);
                return false;
            }
        }

        public bool ConfigurePermissions(string tenantId, ConfigPermissions req)
        {
            try
            {
                var rootContext = new RootContext(_configuration, _encryption).Context;

                var role = rootContext.U_Roles.FirstOrDefault(ptr => ptr.Id == req.RoleId && ptr.TenantId == tenantId && !ptr.IsDelete);

                if (role == null)
                {
                    throw new IboxLog($"Not found role Id: {req.RoleId}", tenantId);
                }

                var permissionsKey = System.Text.Json.JsonSerializer.Serialize(req.PermissionsKey);

                EntityAction.TryUpdate<U_Role>(role, rootContext, new U_Role()
                {
                    Id = req.RoleId,
                    TenantId = tenantId,
                    Name = role.Name,
                    PermissionsKey = permissionsKey
                });

                rootContext.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog(ex.Message, tenantId, ex);
            }
        }

        public List<Permission> GetAllPermissions()
        {
            try
            {
                var listPermissions = PermissionStore.Permissions
                    .Where(permission => permission.Value.Type == "TENANT")
                    .Select(kv =>
                    {
                        kv.Value.Id = kv.Key;
                        return kv.Value;
                    })
                    .ToList();

                return listPermissions;
            }
            catch (Exception ex)
            {
                throw new IboxLog(ex.Message, "AppLogs", ex);
            }
        }

        public List<dynamic> GetAllRole(string tenantId, int pageNumber)
        {
            using (var rootContext = new RootContext(_configuration, _encryption))
            {
                if (pageNumber == -1)
                {
                    var queryResultPage = rootContext.Context.U_Roles.Where(ptr => !ptr.IsDelete && ptr.TenantId == tenantId).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
                else
                {
                    int numberOfObjectsPerPage = 30;
                    var queryResultPage = rootContext.Context.U_Roles
                        .Where(ptr => ptr.IsDelete && ptr.TenantId == tenantId)
                        .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                        .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();
                    return queryResultPage;
                }
            }
        }

        public string GetConfigurePermissions(string tenantId, ConfigPermissions req)
        {
            var rootContext = new RootContext(_configuration, _encryption).Context;

            var role = rootContext.Context.U_Roles.FirstOrDefault(ptr => ptr.Id == req.RoleId && ptr.TenantId == tenantId && !ptr.IsDelete);

            if (role == null)
            {
                throw new IboxLog($"Not found roleId: {req.RoleId}", tenantId);
            }

            rootContext.Dispose();

            return role.PermissionsKey ?? "[]";
        }
    }
}