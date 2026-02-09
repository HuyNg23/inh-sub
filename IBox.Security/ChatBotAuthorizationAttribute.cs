using IBox.Common.Security;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace IBox.Security
{
    public class ChatBotAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
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

                var xEncryption = context.HttpContext.RequestServices.GetService<IEncryption>();

                var authorization = headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authorization))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var credentialBytes = Convert.FromBase64String(authorization);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                var username = credentials[0];
                var password = credentials[1];

                var tenantid = context.HttpContext.Request.RouteValues["tenantid"]?.ToString();
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid);

                if (configChat == null || username != configChat.UserChatBot)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                if (xEncryption == null)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                if (configChat == null || password != xEncryption.Decrypt(configChat.PassChatBot))
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