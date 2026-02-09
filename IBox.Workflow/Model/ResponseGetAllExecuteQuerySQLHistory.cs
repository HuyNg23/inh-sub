using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class ResponseGetAllExecuteQuerySQLHistory
    {
        private int totalReCords;
        private List<ExecuteQuerySQLHistory>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<ExecuteQuerySQLHistory>? Data { get => data; set => data = value; }
    }

    public class ExecuteQuerySQLHistory
    {
        public DateTime? TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? WorkflowId { get; set; }
        public string? WorkflowName { get; set; }
        public string? Reason { get; set; }
        public string? KeyExecuteRunSQLThirdParty { get; set; }
        public WorkflowExecuteStatus? Status { get; set; }
        public string? Query { get; set; }
        public int? RecordCount { get; set; }
        public TimeSpan? TimeInterval { get; set; }
        public string? SiteRun { get; set; }
        public string? StepName { get; set; }
        public string? StepId { get; set; }
        public string? Address { get; set; }
        public string? Catalog { get; set; }
    }
}