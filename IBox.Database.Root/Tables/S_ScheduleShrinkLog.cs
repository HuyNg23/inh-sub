using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    [Index(nameof(S_ScheduleShrinkLog.Name), IsUnique = true)]
    public class S_ScheduleShrinkLog : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        [MaxLength(2048)]
        [Required]
        public string? Name { get; set; }

        [Required]
        public string? ListCategory { get; set; }

        [Required]
        public TypeScheduleBase? Type { get; set; }

        [MaxLength(2)]
        public string? Day { get; set; }

        [MaxLength(2)]
        public string? Month { get; set; }

        [MaxLength(4)]
        public string? Year { get; set; }

        [MaxLength(2)]
        public string? Hour { get; set; }

        [MaxLength(2)]
        public string? Minute { get; set; }

        [MaxLength(64)]
        public string? Site { get; set; }

        [MaxLength(64)]
        public string? Weekday { get; set; } = string.Empty;

        [Required]
        public MethodShrinkLog? MethodShrinkLog { get; set; }
    }

    public enum TypeScheduleBase
    {
        NumberOfHoursMinutes,
        Daily,
        Weekly,
        Monthly
    }

    public enum MethodShrinkLog
    {
        Backup,
        BackupAndShrink
    }
}