namespace IBox.Schedule.Library.Model
{
    public class TriggerScheduleInfo
    {
        public string? Name { get; set; }
        public string? Group { get; set; }
        public string? CronExpression { get; set; }
        public string? NextFireTime { get; set; }
        public string? PreviousFireTime { get; set; }
        public string? Status { get; set; }
        public string? Result { get; set; }
    }
}