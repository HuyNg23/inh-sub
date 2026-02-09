namespace IBox.ScheduleHistory.Model
{
    public class RequestAllScheduleHistory
    {
        public string? TextSearch { get; set; }
        public int PageNum { get; set; }
        public StatusSearch? StatusSearch { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }

    public enum StatusSearch
    {
        Failed,
        Success,
        None
    }
}