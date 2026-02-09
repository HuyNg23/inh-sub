using IBox.Schedule.Library.HandleExecuteWorkflow;
using Quartz;

namespace IBox.Schedule.Library.Execution
{
    [DisallowConcurrentExecution]
    public class ExecuteWorkflow : IJob
    {
        private readonly IHandleExecWorkflow _handleExecWorkflow;

        public ExecuteWorkflow(IHandleExecWorkflow handleExecWorkflow)
        {
            _handleExecWorkflow = handleExecWorkflow;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _handleExecWorkflow.ExecWorkflow(context);
            return Task.CompletedTask;
        }
    }
}