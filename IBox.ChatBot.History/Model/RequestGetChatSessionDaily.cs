namespace IBox.ChatBot.History.Model
{
    public class RequestGetChatSessionDaily
    {
        public int PageNum { get; set; }
        public string? SessionId { get; set; }
        public string? TenantId { get; set; }
        public string? SenderId { get; set; }
        public string? InteractionCRM { get; set; }
        public string? CustomerName { get; set; }
        public string? InteractionIC { get; set; }
        public string? CustomerIdIC { get; set; }
        public string? Channel { get; set; }
        public string? SubChannel { get; set; }
        public int? IsClose { get; set; }
        public int? IsSupport { get; set; }
        public string CreatedDateFrom { get; set; } = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
        public string CreatedDateTo { get; set; } = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
    }
}