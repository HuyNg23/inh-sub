using Quartz;

namespace IBox.Schedule.Library.HandleShrinkLogDB
{
    public interface IHandleShrinkLog
    {
        void RunShrinkLogDatabase(IJobExecutionContext context);
    }
}