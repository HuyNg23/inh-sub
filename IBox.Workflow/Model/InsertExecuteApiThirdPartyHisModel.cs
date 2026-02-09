using IBox.Database.SQLiteDBChatDay.TableDBHistory;

namespace IBox.Workflow.Model
{
    public class InsertExecuteApiThirdPartyHisModel
    {
        public string? TenantId { get; set; }
        public string? Wfid { get; set; }
        public string? KeyExecuteRunApiThirdParty { get; set; }
        public ExecuteRunApiThirdPartyStatus? Status { get; set; }
        public string? StatusCode { get; set; }
        public string? Url { get; set; }
        public string? Request { get; set; }
        public string? Response { get; set; }
        public string? Headers { get; set; }
        public string? Reason { get; set; }
        public string? TimeInterval { get; set; }
        public string? StepName { get; set; }
        public string? StepId { get; set; }
    }
}