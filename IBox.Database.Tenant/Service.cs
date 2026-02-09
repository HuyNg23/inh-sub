using IBox.Database.Root;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Database.Tenant
{
    public static class Service
    {
        public static IServiceCollection AddServiceTenantDB(this IServiceCollection services)
        {
            services.AddScoped<IIBGlobalTenantConfig, IBGlobalTenantConfig>();
            services.AddScoped<IBContext<TenantContext>, TenantContext>();
            return services;
        }
    }
}