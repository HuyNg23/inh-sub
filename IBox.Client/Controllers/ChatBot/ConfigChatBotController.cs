using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace IBox.Client.Controllers.ChatBot
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class ConfigChatBotController : ActionController<CB_ConfigChat, TenantContext>
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly ICallData _callData;

        public ConfigChatBotController(IBContext<TenantContext> tenantContext, IEncryption encryption, ICallData callData, Common.Objects.IConfiguration configuration) : base(tenantContext, encryption)
        {
            _encryption = encryption;
            _callData = callData;
            _configuration = configuration;
        }

        [IBoxActionPermission("integration-config-chatbot-manage", "integration-config-chatbot-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [HttpGet("ReLoadConfigChatBotOK")]
        [IBoxActionPermission("integration-config-chatbot-manage")]
        public IActionResult ReLoadConfigChatBotOK()
        {
            var Authorize = HttpContext.Request.Headers["Authorization"];
            foreach (var urlChat in IBGlobalConfig.ChatBotServiceIBox)
            {
                _callData.CallAPIReload(urlChat, Authorize.ToString());
            }

            foreach (var urlLog in IBGlobalConfig.LogServiceIBox)
            {
                _callData.CallAPIReload(urlLog, Authorize.ToString());
            }

            return Ok();
        }

        [IBoxActionPermission("integration-config-chatbot-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var configChat = CheckValid(req);

                if (configChat == null)
                {
                    throw new IboxLog("Config Chat is null", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(configChat.IdBOT))
                {
                    throw new IboxLog("Id BOT is not null or emty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                configChat.tenant_id = IBContext.Context.TenantInfo.Id;
                configChat.PassChatBot = _encryption.Encrypt(configChat.PassChatBot);
                req.Body = JsonDocument.Parse(JsonConvert.SerializeObject(configChat)).RootElement;
                var configData = base.IBContext.Context.CB_ConfigChats.FirstOrDefault(x => x.IdBOT == configChat.IdBOT && !x.IsDelete);
                if (configData != null)
                {
                    throw new IboxLog("Config Chat already exist", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var result = base.Create(req);

                return result;
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-chatbot-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var configChat = CheckValid(req);

                if (string.IsNullOrEmpty(configChat.IdBOT))
                {
                    throw new IboxLog("Id BOT is not null or emty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(configChat.PassChatBot))
                {
                    throw new IboxLog("PassChatBot is not null or emty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                configChat.tenant_id = IBContext.Context.TenantInfo.Id;
                configChat.PassChatBot = _encryption.Encrypt(configChat.PassChatBot);
                req.Body = JsonDocument.Parse(JsonConvert.SerializeObject(configChat)).RootElement;
                var configData = base.IBContext.Context.CB_ConfigChats.FirstOrDefault(x => x.Id != configChat.Id && x.IdBOT == configChat.IdBOT && !x.IsDelete);

                if (configData != null)
                {
                    throw new IboxLog("Config chat exist Bot", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var result = base.Update(req);

                return result;
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-chatbot-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            try
            {
                var configChat = CheckValid(req);
                var rootContext = new RootContext(_configuration, _encryption);
                var configData = base.IBContext.Context.CB_ConfigChats.FirstOrDefault(x => x.Id == configChat.Id && !x.IsDelete);

                if (configData == null)
                {
                    throw new IboxLog("Config chat does not exist", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var result = base.Delete(req);

                return result;
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }
    }
}