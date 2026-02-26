namespace IBox.ChatBot.DB.Models.ChatBot
{
    public class SessionObj
    {
        public string? SessionId { get; set; }
        public bool IsSupport { get; set; } = false;
        public string? ThisSite { get; set; }
        public string? ThisSiteIdentification { get; set; } = "";
        public DateTime CreateDate { get; set; }
        public string? Interaction { get; set; }
        public string? ContactId { get; set; } = "";
    }
}