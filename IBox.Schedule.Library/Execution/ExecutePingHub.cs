using IBox.Common.Model;
using IBox.Schedule.Library.HubSchedule;
using Quartz;

namespace IBox.Schedule.Library.Execution
{
    [DisallowConcurrentExecution]
    public class ExecutePingHub : IJob
    {
        private readonly IHandleHubSchedule _handleHubSchedule;

        public ExecutePingHub(IHandleHubSchedule handleHubSchedule)
        {
            _handleHubSchedule = handleHubSchedule;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var typeUserBase = dataMap.GetString("typeUserBase");


            var typeUser = (TypeUserBase)Enum.Parse(typeof(TypeUserBase), typeUserBase, true);


            await _handleHubSchedule.CreateHubSchedule(typeUser);
        }
    }
}