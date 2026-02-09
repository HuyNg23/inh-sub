using IBox.Common.Objects;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Security
{
    public class ICTokenAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                var headers = context.HttpContext.Request.Headers;
                if (!headers.Any(ptr => ptr.Key == "Authorization"))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var _configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();

                var authorization = headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(authorization))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var tenantid = context.HttpContext.Request.RouteValues["tenantid"]?.ToString();
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid);

                if (configChat == null || configChat.IC_Token != authorization)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }
            catch (Exception)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}