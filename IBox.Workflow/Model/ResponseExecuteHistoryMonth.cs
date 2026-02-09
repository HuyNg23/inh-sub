namespace IBox.Workflow.Model
{
    public class ResponseExecuteHistoryMonth
    {
        public string? Date { get; set; }
        public int CountSuccess { get; set; } = 0;
        public int CountFail { get; set; } = 0;
    }
}