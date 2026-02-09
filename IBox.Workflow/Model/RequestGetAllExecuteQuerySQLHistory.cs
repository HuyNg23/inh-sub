using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class RequestGetAllExecuteQuerySQLHistory
    {
        public int PageNum { get; set; }
        public string? Date { get; set; } = string.Empty;
        public string? StartTime { get; set; } = string.Empty;
        public string? EndTime { get; set; } = string.Empty;
        public string? WfId { get; set; } = string.Empty;
        public string? WorkflowName { get; set; } = string.Empty;
        public ExecuteRunApiThirdPartyStatus? Status { get; set; }
        public string? Query { get; set; } = string.Empty;
        public string? SiteRun { get; set; } = string.Empty;
        public string? StepName { get; set; } = string.Empty;
        public string? StepId { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? Catalog { get; set; } = string.Empty;
        public string? TenantId { get; set; } = string.Empty;
        public int? Take { get; set; } = 0;
        public int? Skip { get; set; } = 0;
    }
}