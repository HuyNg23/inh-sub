using IBox.Schedule.ShrinkLog.HandleJobPlan;
using Quartz;

namespace IBox.Schedule.ShrinkLog.Execution
{
    public class ExcutePlanShrinkLog : IJob
    {
        private readonly IHandleJobPlanShrinkLog _handleJobPlan;

        public ExcutePlanShrinkLog(IHandleJobPlanShrinkLog handleJobPlan)
        {
            _handleJobPlan = handleJobPlan;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this._handleJobPlan.ExecuteJobPlanShrinkLog();

            return Task.CompletedTask;
        }
    }
}
