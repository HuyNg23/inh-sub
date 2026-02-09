using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers
{
    [Route("api")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ConfigController : ControllerBase
    {
        private readonly IIBGlobalConfig _globalConfig;
        private readonly Common.Objects.IConfiguration _configuration;

        public ConfigController(IIBGlobalConfig globalConfig, Common.Objects.IConfiguration configuration)
        {
            _globalConfig = globalConfig;
            _configuration = configuration;
        }

        [HttpGet("ReLoadConfig")]
        public IActionResult ReLoadService()
        {
            _globalConfig.SendRequestReload(this.Request);
            return Ok();
        }

        [HttpGet("ReLoadOK")]
        public IActionResult ReLoadOK()
        {
            _globalConfig.OnLoad("RootBEService");
            _globalConfig.LoadConfigRoot();
            return Ok();
        }

        [HttpGet("GetConfiguration")]
        public ResponseForm<dynamic> GetConfiguration()
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }
            return new ResponseForm<dynamic>(() =>
            {
                return new IBGlobalConfigList()
                {
                    Site = $"{IBGlobalConfig.ThisSite} - Root service",
                    Services = IBGlobalConfig.Services,
                };
            });
        }

        [HttpGet("GetAllConfiguration")]
        public ResponseForm<List<IBGlobalConfigList>> GetAllConfiguration()
        {
            return new ResponseForm<List<IBGlobalConfigList>>(() =>
            {
                var dynamics = _globalConfig.GetAllConfiguration(this.Request);
                return dynamics;
            });
        }
    }
}