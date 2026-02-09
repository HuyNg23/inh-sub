namespace IBox.Workflow.Model
{
    public class RequestExecuteHistoryDayOrMonth
    {
        private string? site;

        public string SelectDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string? Site { get => site; set => site = value; }
        public string? TenantId { get; set; }
    }
}