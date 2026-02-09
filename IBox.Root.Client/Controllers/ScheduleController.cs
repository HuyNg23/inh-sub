using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ScheduleController : ActionController<S_ScheduleShrinkLog, RootContext>
    {
        public ScheduleController(IBContext<RootContext> context) : base(context)
        {
        }
    }
}