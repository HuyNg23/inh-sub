namespace IBox.ScheduleHistory.Model
{
    public class ScheduleHistoryLineTime
    {
        public int Hour { get; set; }
        public int CountSuccess { get; set; }
        public int CountFail { get; set; }
    }

    public class GetImPlanHistory
    {
        public string? Site { get; set; }
        public string? SelectDate { get; set; }
    }

    public enum RangeTime
    {
        Day,
        Month
    }
}