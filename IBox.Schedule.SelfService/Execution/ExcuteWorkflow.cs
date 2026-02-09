using IBox.Schedule.SelfService.HandleExecuteWorkflow;
using Quartz;

namespace IBox.Schedule.SelfService
{
    [DisallowConcurrentExecution]
    public class ExcuteWorkflow : IJob
    {
        private readonly IHandleExecWorkflow _handleExecWorkflow;

        public ExcuteWorkflow(IHandleExecWorkflow handleExecWorkflow)
        {
            _handleExecWorkflow = handleExecWorkflow;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this._handleExecWorkflow.ExecWorkflow(context);
            return Task.CompletedTask;
        }
    }
}
