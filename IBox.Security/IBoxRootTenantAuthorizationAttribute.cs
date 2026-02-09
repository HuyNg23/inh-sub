using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IBox.Security
{
    public class IBoxRootTenantAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var rootAuth = new IBoxRootAuthorizationAttribute();
            await rootAuth.OnAuthorizationAsync(context);
            if (context.Result is not UnauthorizedResult)
            {
                return;
            }

            context.Result = null;
            var iBoxAuth = new IBoxAuthorizationAttribute();
            await iBoxAuth.OnAuthorizationAsync(context);
            if (context.Result is UnauthorizedResult)
            {
                return;
            }
        }
    }
}