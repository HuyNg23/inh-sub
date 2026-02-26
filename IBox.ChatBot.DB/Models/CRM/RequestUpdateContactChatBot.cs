namespace IBox.ChatBot.DB.Models.CRM
{
    public class RequestUpdateContactChatBot
    {
        public string? channel { get; set; } = "";
        public string? ContactId { get; set; } = "";
        public string? SenderId { get; set; } = "";
    }
}