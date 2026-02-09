using IBox.Database.Root.Tables;

namespace IBox.Schedule.Library.Model
{
    public class ReqCreateScheduleJob
    {
        public string? TenantId { get; set; }
        public string? CronExpression { get; set; }
        public string? ScheduleId { get; set; }
        public string? WfId { get; set; }
        public string? Site { get; set; }
        public IBox.Database.Tenant.Tables.TypeScheduleBase? TypeSchedule { get; set; }
        public DeploymentType? DeploymentType { get; set; }
        public string? ListCategory { get; set; }
        public MethodShrinkLog? MethodShrinkLog { get; set; }
        public string? JobName { get; set; }
    }
}