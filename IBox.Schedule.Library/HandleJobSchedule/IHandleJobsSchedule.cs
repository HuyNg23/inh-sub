using IBox.Database.Root.Tables;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.Schedule.Library.HandleJobSchedule
{
    public interface IHandleJobsSchedule
    {
        bool StartJobNowTenant(HttpRequest requestContext, S_Schedule req);

        bool StartJobNowRoot(HttpRequest requestContext, S_ScheduleShrinkLog req);

        bool StopJobScheduleTenant(HttpRequest requestContext, ReqJobSchedule req);

        bool StopJobScheduleRoot(HttpRequest requestContext, ReqJobSchedule req);

        bool StopJobScheduleOnRingRoot(ReqJobSchedule req);

        bool RunSpecifiedJobTenant(string tenantId, S_Schedule req);

        bool RunSpecifiedJobRoot(HttpRequest requestContext,S_ScheduleShrinkLog req);

        bool StartJobScheduleRoot(HttpRequest requestContext, ReqJobSchedule req);

        bool StartJobScheduleTenant(HttpRequest requestContext, ReqJobSchedule req);

        List<ResScheduleInSite> GetAllJobRunningTenant(HttpRequest requestContext);

        List<ResScheduleInSite> GetAllJobRunningRoot(HttpRequest requestContext);

        ResScheduleInSite GetAllJobRunningOnRing(string? tenantId = "");

        public void RunScheduleTenant();

        public void RunScheduleRoot();

        public bool DeleteJobSchedule(object req);

        public bool DeleteJobScheduleRoot(object req);

        public void RemoveFileUnZip(string folderPath);
    }
}