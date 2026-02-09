using IBox.Schedule.SelfService.Execution;
using IBox.Schedule.SelfService.HandleExecuteWorkflow;
using IBox.Schedule.SelfService.HandleJobSchedule;
using IBox.Schedule.SelfService.HandleScheduleDaily;
using IBox.Schedule.SelfService.HubScheduler;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Schedule.SelfService
{
    public static class Service
    {
        public static IServiceCollection AddServiceSchedule(this IServiceCollection services)
        {
            services.AddScoped<IExecuteJob, ExecuteJob>();
            services.AddScoped<IHandleJobSchedules, HandleJobSchedules>();
            services.AddScoped<IHandleSchedulesDaily, HandleSchedulesDaily>();
            services.AddScoped<IHubSchedule, HubSchedule>();
            services.AddScoped<IHandleExecWorkflow, HandleExecWorkflow>();
            services.AddScoped<ExcuteWorkflow>();
            services.AddScoped<ExecutePlan>();
            return services;
        }
    }
}
