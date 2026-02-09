using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class RequestGetAllExecuteHistory
    {
        public int PageNum { get; set; }
        public ExecuteRunApiThirdPartyStatus? Status { get; set; }
        public string? Date { get; set; } = string.Empty;
        public string? StartTime { get; set; } = string.Empty;
        public string? EndTime { get; set; } = string.Empty;
        public string? TenantId { get; set; } = string.Empty;
        public string? Request { get; set; } = string.Empty;
        public string? Response { get; set; } = string.Empty;
        public string? WfId { get; set; } = string.Empty;
        public string? WfName { get; set; } = string.Empty;
        public string? SiteRun { get; set; } = string.Empty;
        public int? Skip { get; set; } = 0;
        public int? Take { get; set; } = 0;
    }
}