namespace IBox.Schedule.ShrinkLog.Model
{
    public class TriggerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string CronExpression { get; set; } = string.Empty;
        public string NextFireTime { get; set; } = string.Empty;
        public string PreviousFireTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
    }
}
