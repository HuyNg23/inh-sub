using IBox.Permissions.Model;

namespace IBox.Permissions.Execution
{
    public interface IPermissionsService
    {
        Task<bool> CheckPermissionTenantAsync(string tenantId, string userId, string roleId, string controllerName, IEnumerable<string> actionName);

        List<dynamic> GetAllRole(string tenantId, int pageNumber);

        bool ConfigurePermissions(string tenantId, ConfigPermissions req);

        List<Permission> GetAllPermissions();

        string GetConfigurePermissions(string tenantId, ConfigPermissions req);
    }
}