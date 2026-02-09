using IBox.Database.Tenant.ServicesManager;
using Microsoft.Extensions.DependencyInjection;
namespace IBox.Database.Tenant
{
    public static class Service
    {
        public static IServiceCollection AddServiceGetDataLog(this IServiceCollection services)
        {
            services.AddScoped<IGetDataLogStream, GetDataLogStream>();
            return services;
        }
    }
}