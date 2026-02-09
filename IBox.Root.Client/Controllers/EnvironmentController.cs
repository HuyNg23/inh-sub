using IBox.Client.Business.Model;
using IBox.Common.Objects;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnvironmentController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public EnvironmentController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Check Environment
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("CheckEnvironment")]
        public ResponseForm<object> CheckEnvironment(RequestForm<BEnvironment> req)
        {
            return new ResponseForm<object>(() =>
            {
                var env = req.Body.Init(_serviceProvider);
                return env.CheckEnvironment();
            });
        }

        /// <summary>
        /// Build Environment
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("BuildEnvironment")]
        public ResponseForm<string> BuildEnvironment(RequestForm<BEnvironment.BEnvironmentBuilder> req)
        {
            return new ResponseForm<string>(() =>
            {
                var env = req.Body.Init(_serviceProvider);
                env.ConfigRootDatabase(req.Body);
                return "Success";
            });
        }
    }

    public class ReceiveEventEnvironment
    {
        private string eventName = string.Empty;
        private string stateSite = string.Empty;
        private bool status = false;
        private string messages = string.Empty;

        public string EventName { get => eventName; set => eventName = value; }
        public string StateSite { get => stateSite; set => stateSite = value; }
        public bool Status { get => status; set => status = value; }
        public string Messages { get => messages; set => messages = value; }
    }

    public class RegisterHub
    {
        private string connectionId = string.Empty;
        public string ConnectionId { get => connectionId; set => connectionId = value; }
    }
}