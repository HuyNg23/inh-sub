using IBox.Database.Root.Tables;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.Schedule.Library.Commom
{
    public interface ICommonFunction
    {
        /// <summary>
        /// Gọi Api dừng job được chỉ định mà nó đang chạy trên các site schedule cho tenant
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        void CallApiStopJobOnRingTenant(string bodyReq, string authorization);

        /// <summary>
        /// Gọi Api dừng job được chỉ định mà nó đang chạy trên các site schedule cho root
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        void CallApiStopJobOnRingRoot(string bodyReq, string authorization);

        /// <summary>
        /// Gọi Api chạy job được chỉ định trên site schedule cho tenant
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        void CallApiStartJobOnRingTenant(string bodyReq, string authorization);

        /// <summary>
        /// Gọi Api chạy job shrink log được chỉ định trên site cho root
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        void CallApiStartJobOnRingRoot(string bodyReq, string authorization);

        void CallApiRunSpecifiedJobTenant(S_Service service, string authorization, string bodyReq);

        void CallApiRunSpecifiedJobRoot(S_Service service, string authorization, string bodyReq);

        bool CheckScheduleStart();

        void StartSchedule();

        bool CheckExistsJobInSchedule(string nameJobKey);

        void DeleteJobInSchedule(string nameJobKey, string nameTrigger);

        void CreateScheduleJobTenant(ReqCreateScheduleJob req);

        void DeleteJobSchedule(S_Schedule schedule);

        void CreateScheduleJobRoot(ReqCreateScheduleJob req);

        List<ResScheduleInSite> GetAllJobRunning(HttpRequest requestContext, List<string> servers);

        ResScheduleInSite GetAllJobRunningOnRing(string? tenantId = "");

        string BuildUrlHub(S_Service s_Service);

        string FormatNameJobKey(string jobName);

        string FormatNameTrigger(string scheduleId, string site, string tenantId);

        ScheduleBase ConvertToScheduleBase(S_ScheduleShrinkLog schedule);

        ScheduleBase ConvertToScheduleBase(S_Schedule schedule);

        string GetCronExpression(ScheduleBase schedule);
    }
}