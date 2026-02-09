using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class RequestGetAllExecuteThirdPartyHistory
    {
        public int PageNum { get; set; }
        public string? Date { get; set; } = string.Empty;
        public string? StartTime { get; set; } = string.Empty;
        public string? EndTime { get; set; } = string.Empty;
        public string? WorkflowName { get; set; } = string.Empty;
        public string? StepName { get; set; } = string.Empty;
        public string? StepId { get; set; } = string.Empty;
        public string? StatusCode { get; set; } = string.Empty;
        public string? Site { get; set; } = string.Empty;
        public string? Url { get; set; } = string.Empty;
        public string? Request { get; set; } = string.Empty;
        public string? Response { get; set; } = string.Empty;
        public string? TenantId { get; set; } = string.Empty;
        public string? WfId { get; set; } = string.Empty;
        public int? Skip { get; set; } = 0;
        public int? Take { get; set; } = 0;
        public ExecuteRunApiThirdPartyStatus? StatusSearch { get; set; }
    }
}