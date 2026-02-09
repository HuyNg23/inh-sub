using IBox.Common.Objects;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.SelfService.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.Schedule.SelfService.HandleJobSchedule
{
    public interface IHandleJobSchedules
    {
        /// <summary>
        /// Dừng công việc đang chạy
        /// </summary>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StopJobSchedule(HttpRequest requestContext, RequestJobSchedule requestJobSchedule);

        /// <summary>
        /// Khởi động ngay công việc
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StartJobNow(HttpRequest requestContext, RequestForm<S_Schedule> requestJobSchedule);

        /// <summary>
        /// Dừng công việc đang chạy trên site
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StopJobScheduleOnRing(string tenantId, RequestJobSchedule requestJobSchedule);

        /// <summary>
        /// Khởi động lại công việc đã tắt
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        bool StartJobSchedule(HttpRequest requestContext, RequestJobSchedule req);

        /// <summary>
        /// Khởi động lại công việc đã tắt trên site
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StartJobScheduleOnRing(string tenantId, RequestJobSchedule requestJobSchedule);

        /// <summary>
        /// Lấy danh sách Job đang chạy hoặc đã stop trên schedule trong ngày
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="requestAllScheduleHistory"></param>
        /// <returns></returns>
        ResponseAllImplementationPlans AllImplementationPlansDuringDay(string tenantID, RequestAllImplementationPlansDuringDay requestAllScheduleHistory);

        /// <summary>
        /// Khởi động job trên schedule
        /// </summary>
        /// <param name="ternantId"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        bool RunSpecifiedJob(string ternantId, RequestForm<S_Schedule> req);

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên các site
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        List<ScheduleInSite> GetAllJobRunning(HttpRequest requestContext);

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên site
        /// </summary>
        /// <returns></returns>
        ScheduleInSite GetAllJobRunningOnRing();
    }
}
