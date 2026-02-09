using Quartz;

namespace IBox.Schedule.Library.HandleExecuteWorkflow
{
    public interface IHandleExecWorkflow
    {
        void ExecWorkflow(IJobExecutionContext context);
    }
}