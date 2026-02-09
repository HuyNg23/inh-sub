namespace IBox.ScheduleHistory.Model
{
    public class ResponseAllScheduleHistory
    {
        public int TotalReCords { get; set; }
        public List<AllScheduleHistory>? Data { get; set; }
    }
}