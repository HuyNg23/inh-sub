namespace IBox.ChatBot.DB.Models.ChatBot
{
    public class TagModel
    {
        public List<TagSub>? Data { get; set; } = new List<TagSub>();
    }

    public class TagSub
    {
        public string? tag { get; set; } = "";
        public string? sessionId { get; set; } = "";
        public string? senderId { get; set; } = "";
        public string? channel { get; set; } = "";
        public string? subChannel { get; set; } = "";
        public string? customerName { get; set; } = "";
        public string? contactId { get; set; } = "";
    }
}