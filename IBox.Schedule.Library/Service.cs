using IBox.Schedule.Library.Commom;
using IBox.Schedule.Library.HandleExecuteWorkflow;
using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Schedule.Library.HandleShrinkLogDB;
using IBox.Schedule.Library.HandleSqlDependency;
using IBox.Schedule.Library.HubSchedule;
using IBox.Schedule.Library.ShrinkLogConnectionDB;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Schedule.Library
{
    public static class Service
    {
        public static IServiceCollection AddServiceScheduleLibrary(this IServiceCollection services)
        {
            services.AddScoped<IHandleJobsSchedule, HandleJobsSchedule>();
            services.AddScoped<ICommonFunction, CommonFunction>();
            services.AddScoped<IHandleExecWorkflow, HandleExecWorkflow>();
            services.AddScoped<IHandleHubSchedule, HandleHubSchedule>();
            services.AddScoped<IHandleShrinkLog, HandleShrinkLog>();
            services.AddScoped<IConnectionShrinkLog, ConnectionShrinkLog>();
            services.AddScoped<ISqlDependencyDatabase, SqlDependencyDatabase>();
            return services;
        }
    }
}