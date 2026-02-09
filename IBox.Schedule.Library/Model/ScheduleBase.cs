namespace IBox.Schedule.Library.Model
{
    public class ScheduleBase
    {
        public TypeScheduleBase? Type { get; set; }
        public string? Hour { get; set; }
        public string? Minute { get; set; }
        public string? Day { get; set; }
        public string? Weekday { get; set; }
        public string? Site { get; set; }
        public string? Id { get; set; }
    }

    public enum TypeScheduleBase
    {
        NumberOfHoursMinutes,
        Daily,
        Weekly,
        Monthly
    }
}