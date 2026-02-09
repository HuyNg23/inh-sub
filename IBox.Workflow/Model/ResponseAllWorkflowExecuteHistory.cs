using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class ResponseAllWorkflowExecuteHistory
    {
        private int totalReCords;
        private List<WorkflowExecuteHistory>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<WorkflowExecuteHistory>? Data { get => data; set => data = value; }
    }

    public class WorkflowExecuteHistory
    {
        public WorkflowExecuteStatus? Status { get; set; }
        public string? WorkflowId { get; set; }
        public DateTime? TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public TimeSpan? TimeInterval { get; set; }
        public string? KeyExecuteRunWorkFlow { get; set; }
        public string? Reason { get; set; }
        public string? WorkflowName { get; set; }
        public string? SiteRun { get; set; }
        public string? Header { get; set; }
        public string? RequestWF { get; set; }
        public string? ResultWF { get; set; }
    }
}