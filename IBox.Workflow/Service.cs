using IBox.Workflow.Execution;
using IBox.Workflow.Model;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Workflow
{
    public static class Service
    {
        public static IServiceCollection AddServiceWorkflow(this IServiceCollection services)
        {
            services.AddScoped<IModelControl, ModelControl>();
            services.AddScoped<IWorkflowControl, WorkflowControl>();
            services.AddScoped<IBaseObjectBuilder, BaseObjectBuilder>();
            return services;
        }
    }
}