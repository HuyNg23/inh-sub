using Quartz;

namespace IBox.Schedule.SelfService.HandleExecuteWorkflow
{
    public interface IHandleExecWorkflow
    {
        void ExecWorkflow(IJobExecutionContext context);
    }
}
