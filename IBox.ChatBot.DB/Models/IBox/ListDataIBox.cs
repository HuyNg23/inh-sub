namespace IBox.ChatBot.DB.Models.CreateFile.IBox
{
    public class ListDataIBox
    {
        public string? bot_code { get; set; }
        public string? sender_id { get; set; }
        public string? sender_name { get; set; }
        public string? channel { get; set; }
        public string? sub_channel { get; set; }
        public List<MessageIBox>? message { get; set; }
    }

    public class MessageIBox
    {
        private string timestamp1 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public string? id { get; set; }
        public int BOT { get; set; }
        public string? type { get; set; }
        public string? text { get; set; }
        public List<Carousel_cardsIBox>? carousel_cards { get; set; }
        public List<ButtonsIBox>? buttons { get; set; }
        public FileIC? fileIc { get; set; }
        public string timestamp { get => timestamp1; set => timestamp1 = value; }
        public string ThisSite { get; set; } = "";
    }

    public class FileIC
    {
        public string? url { get; set; }
        public string? fileName { get; set; }
    }

    public class Carousel_cardsIBox
    {
        public string? title { get; set; }
        public string? subtitle { get; set; }
        public string? item_url { get; set; }
        public string? image_url { get; set; }
        public List<ButtonsIBox>? buttons { get; set; }
    }

    public class ButtonsIBox
    {
        public string? title { get; set; }
        public string? payload { get; set; }
    }
}