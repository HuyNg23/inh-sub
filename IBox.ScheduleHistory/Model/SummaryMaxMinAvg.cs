namespace IBox.ScheduleHistory.Model
{
    public class SummaryMaxMinAvg
    {
        public SumaryMax? SumaryMax { get; set; }
        public SumaryMin? SumaryMin { get; set; }

        public int AvgCount { get; set; }
    }

    public class SumaryMax
    {
        public string TimeMax { get; set; } = string.Empty;
        public int CountMax { get; set; }
    }

    public class SumaryMin
    {
        public string TimeMin { get; set; } = string.Empty;
        public int CountMin { get; set; }
    }
}