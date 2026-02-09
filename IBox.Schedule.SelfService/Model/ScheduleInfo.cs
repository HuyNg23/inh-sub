namespace IBox.Schedule.SelfService.Model
{
    public class ScheduleInfo
    {
        private JobKeyInfo jobKey;
        private TriggerInfo trigger;

        public JobKeyInfo JobKey { get => jobKey; set => jobKey = value; }
        public TriggerInfo Trigger { get => trigger; set => trigger = value; }
    }
}
