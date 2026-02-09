using IBox.Database.Tenant.Tables;

namespace IBox.Schedule.SelfService.Model
{
    public class ResponseAllImplementationPlans
    {
        private int totalReCords;
        private List<S_ImplementationPlan>? data;
        public int TotalReCords { get => totalReCords; set => totalReCords = value; }
        public List<S_ImplementationPlan>? Data { get => data; set => data = value; }
    }
}
