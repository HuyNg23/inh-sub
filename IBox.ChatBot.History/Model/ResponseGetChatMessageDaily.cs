namespace IBox.ChatBot.History.Model
{
    public class ResponseGetChatMessageDaily
    {
        private int totalReCords;
        private List<GetChatMessageDaily>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<GetChatMessageDaily>? Data { get => data; set => data = value; }
    }

    public class GetChatMessageDaily
    {
        public string? Id { get; set; }
        public string? SessionId { get; set; }
        public string? MessageContent { get; set; }
        public bool? IsInputIC { get; set; }
        public string? Site { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }
}