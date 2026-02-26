namespace IBox.ChatBot.DB.Models.CreateFile.IBox
{
    public class RequestShowData
    {
        public string tenantId { get; set; } = "";
        public string? senderId { get; set; }
        public string? sessionId { get; set; }
        public string? idBot { get; set; }
        public string? channel { get; set; } = "";
        public string? customerName { get; set; } = "";
        public string? subchannel { get; set; } = "";
        public string dateTimeCurrent { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }
}