namespace IBox.ChatBot.DB.Models.CreateFile.IBox
{
    public class ShowDataModel
    {
        public string? bot_code { get; set; }
        public string? sender_id { get; set; }
        public string? sender_name { get; set; }
        public string? channel { get; set; }
        public string? sub_channel { get; set; }
        public List<MessageShow>? message { get; set; }
    }

    public class MessageShow
    {
        public string? id { get; set; }
        public int BOT { get; set; }
        public string? type { get; set; }
        public string? text { get; set; }
        public string? timestamp { get; set; }
        public List<Carousel>? carousel_cards { get; set; }
        public List<ButtonsBOT>? buttons { get; set; }
        public ContenBOT? content { get; set; }
        public string? urlfile { get; set; }
    }

    public class Carousel
    {
        public string? title { get; set; }
        public string? subtitle { get; set; }
        public string? item_url { get; set; }
        public string? image_url { get; set; }
        public List<ButtonsBOT>? buttons { get; set; }
    }

    public class ButtonsBOT
    {
        public string? title { get; set; }
        public string? payload { get; set; }
    }

    public class ContenBOT
    {
        public string? title { get; set; }
    }
}