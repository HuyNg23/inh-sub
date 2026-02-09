using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.StoreService.Controllers
{
    [Route("api")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ConfigController : ControllerBase
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IIBGlobalConfig _globalConfig;

        public ConfigController(Common.Objects.IConfiguration configuration, IIBGlobalConfig globalConfig)
        {
            _configuration = configuration;
            _globalConfig = globalConfig;
        }

        [HttpGet("ReLoadOK")]
        public IActionResult ReLoadOK()
        {
            _globalConfig.OnLoad("StoreService");
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
                return new
                {
                    site = $"{IBGlobalConfig.ThisSite} - Store service",
                    services = IBGlobalConfig.Services,
                };
            });
        }
    }
}