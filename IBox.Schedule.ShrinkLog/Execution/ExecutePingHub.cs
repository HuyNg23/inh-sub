using IBox.Schedule.ShrinkLog.HubShrinkLogDB;
using Quartz;

namespace IBox.Schedule.ShrinkLog.Execution
{
    public class ExecutePingHub : IJob
    {
        private readonly IHubShrinkLog _hubShrinkLog;

        public ExecutePingHub(IHubShrinkLog hubShrinkLog)
        {
            _hubShrinkLog = hubShrinkLog;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await this._hubShrinkLog.CreateHubSchedule();
        }
    }
}
