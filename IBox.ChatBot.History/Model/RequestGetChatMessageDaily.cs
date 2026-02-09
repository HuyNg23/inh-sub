namespace IBox.ChatBot.History.Model
{
    public class RequestGetChatMessageDaily
    {
        public int PageNum { get; set; } = 0;
        public string? TenantId { get; set; }
        public string? SessionId { get; set; }
        public string? SenderId { get; set; } = "";
        public string? DateTimeCurrent { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }
}