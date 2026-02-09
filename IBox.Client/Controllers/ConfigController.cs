using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers
{
    [Route("api")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ConfigController : ControllerBase
    {
        private readonly Common.Objects.IConfiguration Configuration;
        private readonly IIBGlobalConfig globalConfig;

        public ConfigController(IIBGlobalConfig globalConfig, Common.Objects.IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.globalConfig = globalConfig;
        }

        [HttpGet("ReLoadOK")]
        public IActionResult ReLoadOK()
        {
            globalConfig.OnLoad("ClientBEService");
            globalConfig.LoadConfigRoot();
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
                return new
                {
                    site = $"{IBGlobalConfig.ThisSite} - Client service",
                    services = IBGlobalConfig.Services,
                };
            });
        }
    }
}