using IBox.Database.Root.Tables;

namespace IBox.Schedule.ShrinkLog.Model
{
    public class ResAllPlansDuringDay
    {
        public int TotalReCords { get; set; }
        public List<S_PlanShrinkLog>? Data { get; set; }
    }
}
