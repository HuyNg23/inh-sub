namespace IBox.ChatBot.History.Model
{
    public class ResponseHistoryZipFile
    {
        public int TotalReCords { get; set; }
        public List<HistoryZipFile>? Data { get; set; }
    }

    public class HistoryZipFile
    {
        public string? Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? NameFile { get; set; }
        public string? PathFile { get; set; }
        public string? ListFileZip { get; set; }
        public string? ThisSite { get; set; }
    }
}