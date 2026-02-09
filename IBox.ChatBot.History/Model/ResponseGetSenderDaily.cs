namespace IBox.ChatBot.History.Model
{
    public class ResponseGetSenderDaily
    {
        private int totalReCords;
        private List<ResponseGetSender>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<ResponseGetSender>? Data { get => data; set => data = value; }
    }

    public class ResponseGetSender
    {
        public string SenderId { get; set; } = "";
        public string? CustomerInfo { get; set; } = "";
        public string? cif_list_data { get; set; } = "";
        public string? Phone { get; set; } = "";
        public string? ContactId { get; set; } = "";
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public string ThisSite { get; set; } = "";
    }
}