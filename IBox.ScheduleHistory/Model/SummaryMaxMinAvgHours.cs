namespace IBox.ScheduleHistory.Model
{
    public class SummaryMaxMinAvgHours
    {
        public int HourOfDay { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public double AvgCount { get; set; }
    }
}