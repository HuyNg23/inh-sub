using IBox.Database.Root.Tables;
using IBox.Schedule.ShrinkLog.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.Schedule.ShrinkLog.HandleJobPlan
{
    public interface IHandleJobPlanShrinkLog
    {
        /// <summary>
        /// Thực hiện tạo job trên schedule từ plan
        /// </summary>
        void ExecuteJobPlanShrinkLog();

        /// <summary>
        /// Chạy job ngay lập tức
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        bool StartJobNow(HttpRequest requestContext, S_ScheduleShrinkLog req);

        /// <summary>
        /// Lấy tất cả các job đang chạy thực tế trên schedule của tất cả các server root
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        List<ScheduleInSite> GetAllJobRunning(HttpRequest requestContext);

        /// <summary>
        /// Lấy danh sách job đang chạy thực tế trên schedule trên server
        /// </summary>
        /// <returns></returns>
        ScheduleInSite GetAllJobRunningOnRing();

        /// <summary>
        /// Dừng job schedule đang chạy
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StopJobSchedule(HttpRequest requestContext, RequestJobSchedule requestJobSchedule);

        /// <summary>
        /// Bật lại job schedule
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        bool StartJobSchedule(HttpRequest requestContext, RequestJobSchedule req);

        /// <summary>
        /// Dừng công việc đang chạy trên site
        /// </summary>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        bool StopJobScheduleOnRing(RequestJobSchedule requestJobSchedule);

        /// <summary>
        /// Chạy ngay job được chỉ định
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        bool RunSpecifiedJob(S_ScheduleShrinkLog req);

        /// <summary>
        /// Khởi động lại công việc đã tắt trên site
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        bool StartJobScheduleOnRing(RequestJobSchedule req);
    }
}
