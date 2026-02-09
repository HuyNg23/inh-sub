using IBox.Schedule.Library.HandleShrinkLogDB;
using Quartz;

namespace IBox.Schedule.Library.Execution
{
    public class ExecuteShrinkLog : IJob
    {
        private readonly IHandleShrinkLog _handleShrinkLogDB;

        public ExecuteShrinkLog(IHandleShrinkLog handleShrinkLogDB)
        {
            _handleShrinkLogDB = handleShrinkLogDB;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _handleShrinkLogDB.RunShrinkLogDatabase(context);
            return Task.CompletedTask;
        }
    }
}