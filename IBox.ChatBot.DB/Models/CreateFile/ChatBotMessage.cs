namespace IBox.ChatBot.DB.Models.CreateFile
{
    public class ChatBotMessage
    {
        public class Sender
        {
            public string? id { get; set; }
            public string? name { get; set; }
        }

        public class Button
        {
            public string? title { get; set; }
            public string? payload { get; set; }
            public string? url { get; set; }
            public string? phone_number { get; set; }
        }

        public class Content
        {
            public string? text { get; set; }
            public string? title { get; set; }
            public string? url { get; set; }
            public string? channel_file_type { get; set; }
            public string? file_id { get; set; }
            public string? file_name { get; set; }
            public List<Button>? buttons { get; set; }
            public List<CarouselBOT>? carousel_cards { get; set; }
            public List<Tag>? tags { get; set; }
        }

        public class Tag
        {
            public string? tag_code { get; set; }
            public string? tag_name { get; set; }
        }

        public class CarouselBOT
        {
            public string? title { get; set; }
            public string? subtitle { get; set; }
            public string? item_url { get; set; }
            public string? image_url { get; set; }
            public List<Button>? buttons { get; set; }
        }

        public class Message
        {
            public string? type { get; set; }
            public Content? content { get; set; }
        }

        public class Data
        {
            public Sender? sender { get; set; }
            public string? channel { get; set; } = "";
            public string? sub_channel { get; set; } = "";
            public string? chatLogs { get; set; }
            public Message? message { get; set; }
            public string? channel_message_id { get; set; }
            public string? session_id { get; set; }
            public string? sender_id { get; set; }
            public string? description { get; set; }
            public List<ChatLog>? chat_logs { get; set; }
        }

        public class Supporter
        {
            public string? name { get; set; }
            public string? email { get; set; }
            public int? id { get; set; }
        }

        public class SenderAttributes
        {
            public string? channel { get; set; }
            public string? collection_id { get; set; }
            public DateTime? created { get; set; }
            public long? last_user_interactive_time { get; set; }
            public string? sender_name { get; set; }
            public int? id_ai { get; set; }
            public DateTime? updated { get; set; }
            public string? sender_id { get; set; }
            public string? _id { get; set; }
        }

        public class ChatLog
        {
            public string? _id { get; set; }
            public string? message { get; set; }
            public int? source { get; set; }
            public int? type { get; set; }
            public DateTime? created_time { get; set; }
            public string? extra { get; set; }
        }

        public class RootChatBot
        {
            public string? id { get; set; }
            public string? bot_code { get; set; }
            public string? @event { get; set; }
            public Data? data { get; set; }
            public long timestamp { get; set; }
            public Supporter? supporter { get; set; }
            public string? status { get; set; }
            public string? chatLog_id { get; set; }
            public string? error_message { get; set; }
            public string? channel_message_id { get; set; }
            public string? request_id { get; set; }

            public string? session_id { get; set; }
            public string? sender_name { get; set; }
            public string? sender_id { get; set; }
            public string? phonenumber { get; set; }
            public string? email { get; set; }
            public DateTime? created_time { get; set; }
            public DateTime? closed_time { get; set; }
            public SenderAttributes? sender_attributes { get; set; }
        }
    }
}