using IBox.Common.Security;
using IBox.Permissions.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Security
{
    public class IBoxPermissionsAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var headers = context.HttpContext.Request.Headers;

            if (!headers.Any(ptr => ptr.Key == "Authorization"))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var xEncryption = context.HttpContext.RequestServices.GetService<IEncryption>();
            var permissionsTenant = context.HttpContext.RequestServices.GetService<IPermissionsService>();

            if (xEncryption == null || permissionsTenant == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var authorization = headers["Authorization"].ToString();
            var jwt = xEncryption.JWTDecode(authorization);

            string roleId = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "Role").Value.ToString() ?? string.Empty;
            string tenantId = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "Tenant").Value.ToString() ?? string.Empty;
            string userId = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "UserId").Value.ToString() ?? string.Empty;

            var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? string.Empty;

            var actionPermissionAttribute = context.ActionDescriptor.EndpointMetadata
                .OfType<IBoxActionPermissionAttribute>()
                .FirstOrDefault();

            var actionNames = actionPermissionAttribute?.ActionNames
                  ?? new List<string> { context.ActionDescriptor.RouteValues["action"] ?? string.Empty };

            bool hasPermission = await permissionsTenant.CheckPermissionTenantAsync(tenantId, userId, roleId, controllerName, actionNames);

            if (!hasPermission)
            {
                context.Result = new StatusCodeResult(403);
                return;
            }
        }
    }
}