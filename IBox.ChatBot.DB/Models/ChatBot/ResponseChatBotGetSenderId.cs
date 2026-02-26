using IBox.Database.SQLiteDBChatDay;

namespace IBox.ChatBot.DB.Models.ChatBot
{
    public class ResponseChatBotGetSenderId
    {
        public string? SessionId { get; set; } = Guid.NewGuid().ToString();
        public string? SenderId { get; set; } = "";
        public bool? IsClose { get; set; } = false;
        public bool? IsSync { get; set; } = false;
        public string? InteractionCRM { get; set; } = "";
        public string? Channel { get; set; } = "";
        public string? SubChannel { get; set; } = "";
        public bool? IsSupport { get; set; } = false;
        public DateTime? DateMessageLast { get; set; } = DateTime.Now;
        public List<ChatMessageDaily>? chatMessageDailys { get; set; }
    }
}