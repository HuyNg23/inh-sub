namespace IBox.ChatBot.DB.Models.FPT
{
    public class SendMessageUploadModel
    {
        public string? channel { get; set; }
        public string? sender_id { get; set; }
        public Message1? message { get; set; }
    }

    public class Message1
    {
        public string? type { get; set; }
        public Content1? content { get; set; }
    }

    public class Content1
    {
        public string? url { get; set; }
        public string? file_name { get; set; }
    }
}