namespace IBox.Schedule.Library.Model
{
    public class ScheduleInfo
    {
        public JobKeyScheduleInfo? JobKey { get; set; }
        public TriggerScheduleInfo? Trigger { get; set; }
    }
}