using IBox.Schedule.Library.HandleJobSchedule;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Schedule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class ScheduleDailyController : ControllerBase
    {
        private readonly IHandleJobsSchedule _handleJobsSchedule;

        public ScheduleDailyController(IHandleJobsSchedule handleJobsSchedule)
        {
            _handleJobsSchedule = handleJobsSchedule;
        }
    }
}