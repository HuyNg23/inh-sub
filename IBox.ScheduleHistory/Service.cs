using IBox.ScheduleHistory.Execution;
using IBox.ScheduleHistory.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.ScheduleHistory
{
    public static class Service
    {
        public static IServiceCollection AddServiceScheduleHistories(this IServiceCollection services)
        {
            services.AddScoped<IExecuteImplementationHistories, ExecuteImplementationHistories>();
            return services;
        }
    }
}