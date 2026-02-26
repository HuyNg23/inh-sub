namespace IBox.ChatBot.DB.Models.CreateFile.CRM.Respone
{
    public class ResponseCreateCRM
    {
        public string? data { get; set; }
        public string? message { get; set; }
        public string? InteractionId { get; set; }
        public string? ContactId { get; set; }
        public string? SessionId { get; set; }
        public int? status_code { get; set; }
    }
}