using Microsoft.Extensions.DependencyInjection;

namespace IBox.Database.Root
{
    public static class Service
    {
        public static IServiceCollection AddServiceRootDB(this IServiceCollection services)
        {
            services.AddScoped<IBContext<RootContext>, RootContext>();
            services.AddScoped<IIBGlobalConfig, IBGlobalConfig>();
            return services;
        }
    }
}