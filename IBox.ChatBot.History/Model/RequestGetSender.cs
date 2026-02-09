namespace IBox.ChatBot.History.Model
{
    public class RequestGetSender
    {
        public int PageNum { get; set; } = 0;
        public string? SenderId { get; set; } = "";
        public string? TenantId { get; set; } = "";
        public string? CRMContactID { get; set; } = "";
        public string? ThisSite { get; set; } = "";
        public string? Phone { get; set; } = "";
        public string? CifEncryp { get; set; } = "";
        public string? cif_list_data { get; set; } = "";
        public string? CreatedDateFrom { get; set; } = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
        public string? CreatedDateTo { get; set; } = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
    }
}