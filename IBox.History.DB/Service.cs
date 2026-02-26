using IBox.History.DB.WorkflowAndApiThirdPartyHistory;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.History.DB
{
    public static class Service
    {
        public static IServiceCollection AddServiceHistoryDB(this IServiceCollection services)
        {
            services.AddScoped<IHandelWFAndAPI, HandelWFAndAPI>();
            return services;
        }
    }
}