using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Security
{
    public class IBoxRootAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var headers = context.HttpContext.Request.Headers;
            if (!headers.Any(ptr => ptr.Key == "Authorization"))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var xEncryption = context.HttpContext.RequestServices.GetService<IEncryption>();
            var _configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var rootContext = context.HttpContext.RequestServices.GetService<IBContext<RootContext>>();

            if (xEncryption == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (rootContext == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var authorization = headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorization))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var jwt = xEncryption.JWTDecode(authorization);
            if (jwt == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            string exp = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "exp").Value.ToString() ?? string.Empty;
            string user = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "User").Value.ToString() ?? string.Empty;
            string security = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "Security").Value.ToString() ?? string.Empty;
            string tenant = jwt.Payload.FirstOrDefault(ptr => ptr.Key == "Tenant").Value.ToString() ?? string.Empty;
            headers[RequestHeaderKey.User.ToString()] = user;
            headers[RequestHeaderKey.Security.ToString()] = security;
            headers[RequestHeaderKey.Tenant.ToString()] = tenant;

            if (tenant != "ROOT")
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (string.IsNullOrEmpty(exp))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var dtNow = DateTime.Now;
            DateTimeOffset dto = new DateTimeOffset(dtNow.ToLocalTime());
            var timeInSeconds = dto.ToUnixTimeSeconds();

            if (timeInSeconds >= Int64.Parse(exp))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (security != xEncryption.SHAEncode(tenant + user + exp))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var userSession = rootContext.Context.U_Users.FirstOrDefault(ptr => ptr.UserName == user);
            if (userSession == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (userSession.SKey != security)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (userSession.SKey_Expires < DateTime.Now)
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }
    }
}