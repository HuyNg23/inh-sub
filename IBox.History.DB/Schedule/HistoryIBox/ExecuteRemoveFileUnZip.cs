using IBox.Common.Chat.Configuration;
using IBox.Database.Root;
using IBox.Schedule.Library.HandleJobSchedule;
using Quartz;
using Serilog;

namespace IBox.History.DB.Schedule.HistoryIBox
{
    [DisallowConcurrentExecution]
    public class ExecuteRemoveFileUnZip : IJob
    {
        private readonly IHandleJobsSchedule _handleJobsSchedule;

        public ExecuteRemoveFileUnZip(IHandleJobsSchedule handleJobsSchedule)
        {
            _handleJobsSchedule = handleJobsSchedule;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                foreach (var tenant in IBGlobalConfig.Tenants)
                {
                    string pathDayUnZip = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDayUnZip, tenant.Id);
                    _handleJobsSchedule.RemoveFileUnZip(pathDayUnZip);

                    string pathMonthUnZip = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogMonthUnZip, tenant.Id);
                    _handleJobsSchedule.RemoveFileUnZip(pathMonthUnZip);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ExecuteScheduleReadFileWF : {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}