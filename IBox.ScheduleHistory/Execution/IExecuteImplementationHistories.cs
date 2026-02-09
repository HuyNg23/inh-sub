using IBox.Common.Security;
using IBox.Database.Tenant.Tables;
using IBox.ScheduleHistory.Implementation;
using IBox.ScheduleHistory.Model;

namespace IBox.ScheduleHistory.Execution
{
    public interface IExecuteImplementationHistories : ITenantContext<ExecuteImplementationHistories>
    {
        List<ScheduleHistoryLineTime> GetScheduleHistoryLineTimeDay(GetImPlanHistory getImPlanHistory, string tennantID);

        List<ScheduleHistoryLineTimeMonth> GetScheduleHistoryLineTimeMonth(GetImPlanHistory getImPlanHistory, string tennantID);

        SumScheduleImplementationHistory SumScheduleImplementationHistories(string tenantID);

        ResponseAllScheduleHistory AllScheduleHistories(string tenantID, RequestAllScheduleHistory requestAllScheduleHistory);

        public SummaryMaxMinAvg GetSummaryMaxMinAvg(string tenantId, string selectedDate);

        public SummaryMaxMinAvg GetSummaryMaxMinAvgMonth(string tenantId, string selectedMonth);

        public void CallAPIDeleteJobSchedule(S_Schedule obj, string author);
    }
}