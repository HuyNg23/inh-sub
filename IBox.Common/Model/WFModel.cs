namespace IBox.Common.Model
{
    public class WFModel
    {
        public string? TenantId { get; set; }
        public string? Wfid { get; set; }
        public string? WFName { get; set; }
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public string? Header { get; set; }
        public string? RequestWF { get; set; }
        public string? ResultWF { get; set; }
        public string? KeyExecuteRunWorkFlow { get; set; }
        public string? SiteRun { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModificationDate { get; set; }
        public HashSet<string> SiteCall { get; set; } = new HashSet<string>(); //Site đã có data call đến để đồng bộ
    }
}