using IBox.Schedule.SelfService.HandleScheduleDaily;
using Quartz;

namespace IBox.Schedule.SelfService.Execution
{
    public class ExecuteInsertScheduleToPlan : IJob
    {
        private readonly IHandleSchedulesDaily handleSchedulesDaily;

        public ExecuteInsertScheduleToPlan(IHandleSchedulesDaily handleSchedulesDaily)
        {
            this.handleSchedulesDaily = handleSchedulesDaily;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this.handleSchedulesDaily.InsertScheduleToImplementationPlan();
            return Task.CompletedTask;
        }
    }
}
