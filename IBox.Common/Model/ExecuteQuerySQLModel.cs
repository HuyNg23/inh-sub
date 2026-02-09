namespace IBox.Common.Model
{
    public class ExecuteQuerySQLModel
    {
        public string? TenantId { get; set; }
        public string? WfId { get; set; }
        public string? WFName { get; set; }
        public string? StepName { get; set; }
        public string? StepId { get; set; }
        public string? KeyExecuteRunQuerySQL { get; set; }
        public string? Status { get; set; }
        public string? Address { get; set; }
        public string? Catalog { get; set; }
        public string? Query { get; set; }
        public int? RecordCount { get; set; }
        public string? Reason { get; set; }
        public string? SiteRun { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModificationDate { get; set; }
    }
}