using IBox.Database.Tenant.Tables;

namespace IBox.ScheduleHistory.Model
{
    public class AllScheduleHistory
    {
        public DateTime? TimeStart { get; set; }
        public TimeSpan? TimeRun { get; set; }
        public string? NameSchedule { get; set; }
        public StatusPlan? Status { get; set; }
        public string? Site { get; set; }
        public string? NameWf { get; set; }
        public TypeScheduleBase? Type { get; set; }
        public DateTime? MinTimeStart { get; set; }
        public DateTime? MaxTimeStart { get; set; }
        public string? KeyExecuteRunWorkFlow { get; set; }
        public string? Reason { get; set; }
    }
}