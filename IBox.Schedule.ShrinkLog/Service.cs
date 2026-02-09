using IBox.Schedule.ShrinkLog.ConnectionDB;
using IBox.Schedule.ShrinkLog.HandleJobPlan;
using IBox.Schedule.ShrinkLog.HandleScheduleDaily;
using IBox.Schedule.ShrinkLog.HandleShrinkLogDB;
using IBox.Schedule.ShrinkLog.HubShrinkLogDB;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Schedule.ShrinkLog
{
    public static class Service
    {
        public static IServiceCollection AddServiceSchedule(this IServiceCollection services)
        {
            services.AddScoped<IHandleSchedulesDaily, HandleSchedulesDaily>();
            services.AddScoped<IHandleJobPlanShrinkLog, HandleJobPlanShrinkLog>();
            services.AddScoped<IHandleShrinkLog, HandleShrinkLog>();
            services.AddScoped<IConnectionShrinkLog, ConnectionShrinkLog>();
            services.AddScoped<IHubShrinkLog, HubShrinkLog>();
            return services;
        }
    }
}
