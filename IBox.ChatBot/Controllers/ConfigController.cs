using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IBox.ChatBot.Controllers
{
    [Route("api")]
    [ApiController]
    [IBoxRootTenantAuthorization]
    public class ConfigController : ControllerBase
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IIBGlobalConfig _globalConfig;
        private readonly IIBGlobalTenantConfig _globalTenantConfig;
        private readonly IRestAPI _restAPI;

        public ConfigController(IIBGlobalConfig globalConfig, Common.Objects.IConfiguration configuration, IIBGlobalTenantConfig globalTenantConfig, IRestAPI restAPI)
        {
            _configuration = configuration;
            _globalConfig = globalConfig;
            _globalTenantConfig = globalTenantConfig;
            _restAPI = restAPI;
        }

        [HttpGet("ReLoadOK")]
        public IActionResult ReLoadOK()
        {
            _globalConfig.OnLoad("ChatBotService");
            _globalConfig.LoadConfigRoot();
            _globalTenantConfig.OnLoadConfigChatBot();
            return Ok();
        }

        [HttpGet("ReLoadConfigChatBotOK")]
        public IActionResult ReLoadConfigChatBotOK()
        {
            _globalTenantConfig.OnLoadConfigChatBot();
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
                    site = $"{IBGlobalConfig.ThisSite} - ChatBot service",
                    services = IBGlobalConfig.Services,
                };
            });
        }

        [HttpGet]
        [Route("ShowConfiguration/{idBot}")]
        public ResponseForm<dynamic> ShowConfiguration(string? idBot)
        {
            var token = Request.Headers["Authorization"];
            List<ConfigChat> cB_ConfigChats = new List<ConfigChat>();
            var chatBotService = IBGlobalConfig.ChatBotServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
            foreach (var urlChatBot in chatBotService)
            {
                var result = _restAPI.SendChatBot(new RestAPIRequest()
                {
                    Body = "{}",
                    Headers = new List<RestAPIHeader>()
                      {
                          new RestAPIHeader()
                          {
                               Label = "Content-Type",
                               Value = "application/json"
                          },
                          new RestAPIHeader()
                                {
                                    Label = "Authorization",
                                    Value = token
                                }
                      },
                    Method = "GET",
                    Timeout = 30,
                    Url = $"{urlChatBot}/api/GetConfigurationChatBot/{idBot}"
                });

                if (result == null || string.IsNullOrEmpty(result.Result))
                {
                    continue;
                }

                var configChat = JsonConvert.DeserializeObject<ConfigChat>(result.Result);

                if (configChat != null)
                {
                    cB_ConfigChats.Add(configChat);
                }
            }

            var configChat1 = new ConfigChat()
            {
                ConfigChats = idBot == "All" ? IBGlobalTenantConfig.ConfigChatBots : IBGlobalTenantConfig.ConfigChatBots.Where(ptr => ptr.IdBOT == idBot).ToList(),
                ThisSite = IBGlobalConfig.ThisSite
            };

            cB_ConfigChats.Add(configChat1);

            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
            }

            return new ResponseForm<dynamic>(() =>
            {
                return new
                {
                    cB_ConfigChats
                };
            });
        }

        [HttpGet]
        [Route("GetConfigurationChatBot/{idBot}")]
        public ConfigChat GetConfigurationChatBot(string? idBot)
        {
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
            }

            return new ConfigChat()
            {
                ConfigChats = idBot == "All" ? IBGlobalTenantConfig.ConfigChatBots : IBGlobalTenantConfig.ConfigChatBots.Where(ptr => ptr.IdBOT == idBot).ToList(),
                ThisSite = IBGlobalConfig.ThisSite
            };
        }

        public class ConfigChat
        {
            public List<CB_ConfigChat> ConfigChats { get; set; } = new List<CB_ConfigChat>();
            public string ThisSite { get; set; } = "";
        }
    }
}