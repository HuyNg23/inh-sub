using IBox.Schedule.ShrinkLog.HandleShrinkLogDB;
using Quartz;

namespace IBox.Schedule.ShrinkLog.Execution
{
    public class ExcuteShrinkLog : IJob
    {
        private readonly IHandleShrinkLog _handleShrinkLogDB;

        public ExcuteShrinkLog(IHandleShrinkLog handleShrinkLogDB)
        {
            _handleShrinkLogDB = handleShrinkLogDB;
        }

        public Task Execute(IJobExecutionContext context)
        {
            this._handleShrinkLogDB.RunShrinkLogDatabase(context);
            return Task.CompletedTask;
        }
    }
}
