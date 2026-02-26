using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay.TableDBHistory
{
    public class H_SQLExecuteHistory : BaseTableSQLite
    {
        [MaxLength(256)]
        public string? WfId { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? WfName { get; set; } = string.Empty;

        public string? Reason { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? KeyExecuteRunQuerySQL { get; set; } = string.Empty;

        public string? Status { get; set; } = string.Empty;
        public string? Query { get; set; } = string.Empty;
        public int? RecordCount { get; set; } = 0;

        [MaxLength(256)]
        public string? SiteRun { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? StepName { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? StepId { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Address { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Catalog { get; set; } = string.Empty;
        public int Hour { get; set; }
    }
}