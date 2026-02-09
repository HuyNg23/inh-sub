namespace IBox.ChatBot.History.Model
{
    public class ResponseGetChatSessionDaily
    {
        private int totalReCords = 0;
        private List<GetChatSessionDaily>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<GetChatSessionDaily>? Data { get => data; set => data = value; }
    }

    public class GetChatSessionDaily
    {
        public string? CustomerName { get; set; }
        public string? SessionId { get; set; }
        public string? SenderId { get; set; }
        public bool? IsClose { get; set; }
        public bool? IsSync { get; set; }
        public string? InteractionCRM { get; set; }
        public string? Channel { get; set; }
        public string? SubChannel { get; set; }
        public string? IsSiteClose { get; set; }
        public bool? IsSupport { get; set; }
        public DateTime? DateMessageLast { get; set; }
        public string? Site { get; set; }
        public string? CustomerId { get; set; }
        public string? IdIc { get; set; }
        public string? TenantId { get; set; }
    }
}