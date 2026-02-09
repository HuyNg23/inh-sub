using IBox.Database.Tenant.Tables;

namespace IBox.Schedule.SelfService.HandleScheduleDaily
{
    public interface IHandleSchedulesDaily
    {
        /// <summary>
        /// Thực thi insert Schedule vào ImplementationPlan
        /// </summary>
        /// <returns></returns>
        bool InsertScheduleToImplementationPlan();
    }
}
