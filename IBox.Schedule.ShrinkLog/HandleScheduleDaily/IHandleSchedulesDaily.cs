using IBox.Schedule.ShrinkLog.Model;

namespace IBox.Schedule.ShrinkLog.HandleScheduleDaily
{
    public interface IHandleSchedulesDaily
    {
        bool InsertScheduleToPlan();
        ResAllPlansDuringDay GetAllPlansDuringDay(ReqAllPlansDuringDay reqAllPlansDuringDay);
        void MakePlanForRoot(string thisSite);
        void MakePlanForRoot(string scheduleId, string thisSite);

    }
}
