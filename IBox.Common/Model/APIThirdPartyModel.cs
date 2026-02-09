namespace IBox.Common.Model
{
    public class APIThirdPartyModel
    {
        public string? TenantId { get; set; }
        public string? Wfid { get; set; }
        public string? WFName { get; set; }
        public string? StepName { get; set; }
        public string? StepId { get; set; }
        public string? KeyExecuteRunApiThirdParty { get; set; }
        public string? Status { get; set; }
        public string? StatusCode { get; set; }
        public string? Url { get; set; }
        public string? Request { get; set; }
        public string? Response { get; set; }
        public string? Headers { get; set; }
        public string? Reason { get; set; }
        public string? SiteRun { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModificationDate { get; set; }
    }
}