using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(S_ImplementationHistory.ScheduleID), nameof(S_ImplementationHistory.ImplementationPlanID))]
    public class S_ImplementationHistory : BaseTable
    {
        public StatusPlan? StatusPlan { get; set; }

        [MaxLength(60)]
        public string? ScheduleID { get; set; }

        [MaxLength(60)]
        public string? ImplementationPlanID { get; set; }

        [MaxLength(2048)]
        public string? Reason { get; set; }

        public TypeScheduleBase? Type { get; set; }

        [MaxLength(64)]
        public string? Site { get; set; }

        [MaxLength(64)]
        public string? KeyExecuteRunWorkFlow { get; set; }
    }

    public enum StatusPlan
    {
        Await,
        Running,
        Complete,
        Fail,
        Stop
    }
}