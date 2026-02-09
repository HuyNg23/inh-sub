using IBox.Database.DBC.Factories;
using IBox.Database.DBC.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Database.DBC
{
    public static class Service
    {
        public static IServiceCollection AddServiceDBC(this IServiceCollection services)
        {
            services.AddScoped<IDBConnection, DBConnection>();
            return services;
        }
    }
}