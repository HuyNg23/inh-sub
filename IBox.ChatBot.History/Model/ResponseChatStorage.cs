namespace IBox.ChatBot.History.Model
{
    public class ResponseChatStorage
    {
        private int totalReCords;
        private List<ChatStorages>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<ChatStorages>? Data { get => data; set => data = value; }
    }

    public class ChatStorages
    {
        public string? Id { get; set; } = "";
        public string? SenderId { get; set; } = "";
        public string? StoragePath { get; set; } = "";
        public string? ThisSite { get; set; } = "";
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }
}