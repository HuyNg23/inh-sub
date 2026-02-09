namespace IBox.Schedule.Library.Model
{
    internal class ReqScheduleHistory
    {
        public string? TenantId { get; set; }
        public string? ScheduleId { get; set; }
        public string? Site { get; set; }
        public string? Type { get; set; }
        public string? KeyExecuteRunWorkFlow { get; set; }
    }
}