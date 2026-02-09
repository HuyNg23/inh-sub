namespace IBox.Schedule.ShrinkLog.Model
{
    public class ScheduleInSite
    {
        public string Site { get; set; } = string.Empty;
        public List<ScheduleInfo>? JobSchedules { get; set; }
    }
}
