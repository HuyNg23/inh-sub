namespace IBox.ChatBot.History.Model
{
    public class RequestHistoryZipFile
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? NameFile { get; set; }
        public string? ThisSite { get; set; }
        public int PageNum { get; set; }
    }
}