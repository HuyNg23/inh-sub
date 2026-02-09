using IBox.Schedule.SelfService.HubScheduler;
using Quartz;

namespace IBox.Schedule.SelfService.Execution
{
    public class ExecutePingHub : IJob
    {
        private readonly IHubSchedule _pingSchedule;

        public ExecutePingHub(IHubSchedule pingSchedule)
        {
            this._pingSchedule = pingSchedule;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await this._pingSchedule.CreateHubSchedule();
        }
    }
}
