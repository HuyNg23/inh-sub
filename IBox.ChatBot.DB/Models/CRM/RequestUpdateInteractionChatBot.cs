namespace IBox.ChatBot.DB.Models.CRM
{
    public class RequestUpdateInteractionChatBot
    {
        public string SessionId { get; set; } = "";
        public string InteractionId { get; set; } = "";
        public string SenderId { get; set; } = "";
    }
}