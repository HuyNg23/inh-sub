using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Schedule.Library.Model;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers
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
        /// Chạy công việc ngay lập tức
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StartJobNow")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> StartJobNow(RequestForm<S_ScheduleShrinkLog> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StartJobNowRoot(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Lấy tất cả danh sách công việc đang chạy thực tế trên tất cả các site
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllJobRunning")]
        [IBoxRootAuthorization]
        public ResponseForm<List<ResScheduleInSite>> GetAllJobRunning()
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResScheduleInSite>>(() =>
            {
                return _handleJobsSchedule.GetAllJobRunningRoot(HttpContext.Request);
            });
        }

        /// <summary>
        /// Lấy danh sách công việc đang chạy thực tế trên schedule trên site
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetAllJobRunningOnRing")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> GetAllJobRunningOnRing()
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.GetAllJobRunningOnRing();
            });
        }

        /// <summary>
        /// Dừng công việc shrink log đang chạy
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StopJobSchedule")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> StopJobSchedule(RequestForm<ReqJobSchedule> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StopJobScheduleRoot(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Bật lại công việc shrink log
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StartJobSchedule")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> StartJobSchedule(RequestForm<ReqJobSchedule> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.StartJobScheduleRoot(HttpContext.Request, req.Body);
            });
        }

        /// <summary>
        /// Dừng công việc shrink log đang chạy trên site
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("StopJobScheduleOnRing")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> StopJobScheduleOnRing(object req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.DeleteJobScheduleRoot(req);
            });
        }

        [HttpGet("GetAllJobRoot")]
        public ResponseForm<dynamic> GetAllJobRoot()
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.GetAllJobRunningOnRing();
            });
        }

        [HttpPost("RunSpecifiedJob")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> RunSpecifiedJob(S_ScheduleShrinkLog req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _handleJobsSchedule.RunSpecifiedJobRoot(HttpContext.Request,req);
            });
        }
    }
}