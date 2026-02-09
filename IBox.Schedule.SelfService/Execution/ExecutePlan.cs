using Quartz;
using Serilog;

namespace IBox.Schedule.SelfService.Execution
{
    public class ExecutePlan : IJob
    {
        private readonly IExecuteJob _executeJob;

        public ExecutePlan(IExecuteJob executeJob)
        {
            this._executeJob = executeJob;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                Log.Information("Start execute plan.");
                this._executeJob.RunJobDay();
            }
            catch (Exception ex)
            {
                Log.Error($"ExecutePlan an error has occurred: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    }
}
