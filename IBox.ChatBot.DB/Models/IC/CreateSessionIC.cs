namespace IBox.ChatBot.DB.Models.IC
{
    public class CreateSessionIC
    {
        public string? event_key { get; set; }
        public string? verify_key { get; set; }
        public DataCreateSessionIC? data { get; set; }
    }

    public class DataCreateSessionIC
    {
        public string? id { get; set; }
        public string? customer_id { get; set; }
        public string? user_social_id { get; set; }
        public string? page_social_id { get; set; }
    }
}