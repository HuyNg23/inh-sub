using IBox.DLEx.Execution;
using IBox.DLEx.Implementation;
using IBox.Formatting;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.DLEx
{
    public static class Service
    {
        public static IServiceCollection AddServiceDLLEx(this IServiceCollection services)
        {
            services.AddScoped<IExecuteWF, ExecuteWF>();
            services.AddScoped<IString, IBString>();
            return services;
        }
    }
}