namespace IBox.ChatBot.DB.Models.FPT
{
    public class SendMessageModel
    {
        public string? channel { get; set; }
        public string? sender_id { get; set; }
        public Message? message { get; set; }
    }

    public class Message
    {
        public string? type { get; set; }
        public Content? content { get; set; }
    }

    public class Content
    {
        public string? text { get; set; }
    }
}