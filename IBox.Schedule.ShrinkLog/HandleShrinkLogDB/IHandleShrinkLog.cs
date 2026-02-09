using Quartz;

namespace IBox.Schedule.ShrinkLog.HandleShrinkLogDB
{
    public interface IHandleShrinkLog
    {
        void RunShrinkLogDatabase(IJobExecutionContext context);
    }
}
