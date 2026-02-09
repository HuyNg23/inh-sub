using IBox.Permissions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Permissions
{
    public static class Service
    {
        public static IServiceCollection AddServicePermissionsTenant(this IServiceCollection services)
        {
            services.AddScoped<IPermissionsService, PermissionsService>();
            return services;
        }
    }
}