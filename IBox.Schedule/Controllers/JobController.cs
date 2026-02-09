using IBox.Common.Objects;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Schedule.Library.Model;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Schedule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IHandleJobsSchedule _handleJobsSchedule;

        public JobController(IHandleJobsSchedule handleJobsSchedule)
        {
            _handleJobsSchedule = handleJobsSchedule;
        }

        /// <summary>
        /// Chạy ngay job được chỉ định
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StartJobNow")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public ResponseForm<dynamic> StartJobNow(RequestForm<S_Schedule> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StartJobNowTenant(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Dừng Job đang chạy trên site
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StopJobScheduleOnRing")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public ResponseForm<dynamic> StopJobScheduleOnRing(object req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.DeleteJobSchedule(req);
            });
        }

        /// <summary>
        /// Tạm dừng job đang chạy
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("PauseJob")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public ResponseForm<dynamic> PauseJobSchedule(RequestForm<ReqJobSchedule> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StopJobScheduleTenant(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Bật lại Job đã tắt trước đó
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("ResumeJob")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public ResponseForm<dynamic> ResumeJobSchedule(RequestForm<ReqJobSchedule> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StartJobScheduleTenant(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Chạy ngay job được chỉ định trên site
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("RunSpecifiedJob")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage")]
        public ResponseForm<dynamic> RunSpecifiedJob(S_Schedule req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return _handleJobsSchedule.RunSpecifiedJobTenant(tenantID, req);
            });
        }

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên toàn bộ site
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllJobRunning")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public ResponseForm<List<ResScheduleInSite>> GetAllJobRunning()
        {
            return new ResponseForm<List<ResScheduleInSite>>(() =>
            {
                return _handleJobsSchedule.GetAllJobRunningTenant(HttpContext.Request);
            });
        }

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên site
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllJobRunningOnRing")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public ResponseForm<dynamic> GetAllJobRunningOnRing()
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return _handleJobsSchedule.GetAllJobRunningOnRing(tenantID);
            });
        }

        [HttpPost("DeleteJobSchedule")]
        public void DeleteJobSchedule(object req)
        {
            _handleJobsSchedule.DeleteJobSchedule(req);
        }

        [HttpGet("GetAllJob")]
        public ResponseForm<dynamic> GetAllJob()
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.GetAllJobRunningOnRing();
            });
        }
    }
}