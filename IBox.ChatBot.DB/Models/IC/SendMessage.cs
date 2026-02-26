namespace WebAPI.Model.IC
{
    public class SendMessage
    {
        public string? app_id { get; set; }
        public string? user_id_by_app { get; set; }
        public string? username { get; set; }
        public string? channel { get; set; }
        public Message1? message { get; set; }
    }

    public class Message1
    {
        public string? msg_type { get; set; }
        public string? text { get; set; }
        public string? file_name { get; set; }
        public string? file_url { get; set; }
        public string? file_id { get; set; }
        public Content? content { get; set; }
    }

    public class Content
    {
        public string? text { get; set; }
        public string? file_url { get; set; }
        public string? file_name { get; set; }
        public string? file_id { get; set; }
    }

    public class ReponeSendMessagefEnableBot
    {
        public string? topicName { get; set; }
        public int? partition { get; set; }
        public int? errorCode { get; set; }
        public string? baseOffset { get; set; }
        public string? logAppendTime { get; set; }
        public string? logStartOffset { get; set; }
    }
}