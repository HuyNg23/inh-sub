using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class ResponseAllApiThirdPartyExecuteHistory
    {
        private int totalReCords;
        private List<ApiThirdPartyExecuteHistory>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<ApiThirdPartyExecuteHistory>? Data { get => data; set => data = value; }
    }

    public class ApiThirdPartyExecuteHistory
    {
        private ExecuteRunApiThirdPartyStatus? status;
        private string? workflowId;
        private string? workflowName;
        private string? stepName;
        private string? stepId;
        private DateTime? timeStart;
        private DateTime? timeEnd;
        private TimeSpan? timeInterval;
        private string? reason;
        private string? statusCode;
        private string? url;
        private string? headers;
        private string? request;
        private string? response;
        private string? siteRun;

        public ExecuteRunApiThirdPartyStatus? Status { get => status; set => status = value; }
        public string? WorkflowId { get => workflowId; set => workflowId = value; }
        public string? WorkflowName { get => workflowName; set => workflowName = value; }
        public string? StepName { get => stepName; set => stepName = value; }
        public string? StepId { get => stepId; set => stepId = value; }
        public DateTime? TimeStart { get => timeStart; set => timeStart = value; }
        public DateTime? TimeEnd { get => timeEnd; set => timeEnd = value; }
        public TimeSpan? TimeInterval { get => timeInterval; set => timeInterval = value; }
        public string? Reason { get => reason; set => reason = value; }
        public string? StatusCode { get => statusCode; set => statusCode = value; }
        public string? Url { get => url; set => url = value; }
        public string? Headers { get => headers; set => headers = value; }
        public string? Request { get => request; set => request = value; }
        public string? Response { get => response; set => response = value; }
        public string? SiteRun { get => siteRun; set => siteRun = value; }
    }
}