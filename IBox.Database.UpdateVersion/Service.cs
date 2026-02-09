using Microsoft.Extensions.DependencyInjection;
using IBox.Database.UpdateVersion.Factories;
using IBox.Database.UpdateVersion.Implementation;

namespace IBox.Database.UpdateVersion
{
    public static class Service
    {
        public static IServiceCollection AddServiceUpdateVersionDB(this IServiceCollection services)
        {
            services.AddScoped<ITenantManagementService, TenantManagementService>();
            return services;
        }
    }
}