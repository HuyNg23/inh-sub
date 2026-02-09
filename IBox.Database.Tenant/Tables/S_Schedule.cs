using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(S_Schedule.Day), nameof(S_Schedule.Hour), nameof(S_Schedule.Minute))]
    public class S_Schedule : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string? name;

        private string? wfid;

        private TypeScheduleBase? type;

        private string? day;

        private string? month;

        private string? year;

        private string? hour;

        private string? minute;

        private string? site;

        private string? weekday;

        [MaxLength(2048)]
        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(60)]
        [Required]
        public string? Wfid { get => wfid; set => wfid = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public TypeScheduleBase? Type { get => type; set => type = value; }

        [MaxLength(2)]
        public string? Day { get => day; set => day = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(2)]
        public string? Month { get => month; set => month = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(4)]
        public string? Year { get => year; set => year = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(2)]
        public string? Hour { get => hour; set => hour = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(2)]
        public string? Minute { get => minute; set => minute = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(64)]
        public string? Site { get => site; set => site = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(64)]
        public string? Weekday { get => weekday; set => weekday = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }

    public enum TypeScheduleBase
    {
        NumberOfHoursMinutes,
        Daily,
        Weekly,
        Monthly
    }
}