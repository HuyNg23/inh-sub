namespace IBox.ChatBot.DB.Models.ChatBot
{
    public class DataFileTemp
    {
        public List<DataFile>? Data { get; set; }
    }

    public class DataFile
    {
        public string senderId { get; set; } = "";
        public string? tenantId { get; set; } = "";
        public string? bodyData { get; set; } = "";
        public string? channel { get; set; } = "";
        public string? sub_channel { get; set; } = "";
        public string? name { get; set; } = "";
        public string? bot_code { get; set; } = "";
        public bool isSupport { get; set; } = false;
        public bool isInputIC { get; set; } = false;
        public DateTime? DateMessageLast { get; set; } = DateTime.Now;
    }
}