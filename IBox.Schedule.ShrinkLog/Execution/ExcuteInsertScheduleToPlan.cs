using IBox.Schedule.ShrinkLog.HandleScheduleDaily;
using Quartz;

namespace IBox.Schedule.ShrinkLog.Execution
{
    public class ExcuteInsertScheduleToPlan : IJob
    {
        private readonly IHandleSchedulesDaily _handleSchedulesDaily;

        public ExcuteInsertScheduleToPlan(IHandleSchedulesDaily handleSchedulesDaily)
        {
            this._handleSchedulesDaily = handleSchedulesDaily;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this._handleSchedulesDaily.InsertScheduleToPlan();
            return Task.CompletedTask;
        }
    }
}
