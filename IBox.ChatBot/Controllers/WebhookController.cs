using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using IBox.ChatBot.DB.Models.FPT;
using IBox.ChatBot.DB.Models.IC;
using IBox.ChatBot.Service;
using IBox.ChatBot.Service.CustomerInfo;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Text;
using WebAPI.Model.IC;
using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;

namespace IBox.ChatBot.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IFileProcessing _fileProcessing;
        private readonly IInfoCustomerService _infoCustomerService;

        public WebhookController(Common.Objects.IConfiguration configurations, IFileProcessing fileProcessing, IInfoCustomerService infoCustomerService)
        {
            _configuration = configurations;
            _fileProcessing = fileProcessing;
            _infoCustomerService = infoCustomerService;
        }

        /// <summary>
        /// Nhận data tin nhắn bot gửi đến IC
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [HttpGet]
        [Route("receive-data/{tenantid}")]
        public async Task<ContentResult> ReceiveData(string tenantid)
        {
            ContentResult result = new ContentResult();
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid);
                if (configChat == null)
                {
                    new IboxLog($"ReceiveData configChat is null tenantid: {tenantid}", tenantid, "Error");
                    return result;
                }

                var hub_verify_token = configChat.TokenBOTSendChatBot;
                string param_verify_token = "";
                int hub_challenge = 1;
                if (this.HttpContext.Request.Method == HttpMethod.Get.ToString())
                {
                    foreach (var param in this.HttpContext.Request.Query)
                    {
                        switch (param.Key)
                        {
                            case "hub_verify_token":
                                param_verify_token = param.Value;
                                break;

                            case "hub_challenge":
                                hub_challenge = int.Parse(param.Value);
                                break;

                            default:
                                break;
                        }
                    }
                    if (hub_verify_token == param_verify_token)
                    {
                        result = new ContentResult();
                        result.StatusCode = 200;
                        result.Content = hub_challenge.ToString();
                    }
                    else
                    {
                        result = new ContentResult();
                        result.StatusCode = 401;
                        result.Content = "Unauthorized";
                    }
                    return result;
                }
                else
                {
                    new IboxLog($"Key: {JsonConvert.SerializeObject(this.HttpContext.Request.Query.Keys)}", tenantid, "Info");
                }

                if (!this.Request.Body.CanSeek)
                {
                    this.Request.EnableBuffering();
                }

                var reader = new StreamReader(this.Request.Body, Encoding.UTF8);
                var message = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (!HttpContext.Response.HasStarted)
                {
                    HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
                }

                this.HttpContext.Response.CompleteAsync();

                if (hub_verify_token == param_verify_token || string.IsNullOrEmpty(param_verify_token) == true)
                {
                    try
                    {
                        if (message != null && message != "")
                        {
                            var chatBot = JsonConvert.DeserializeObject<RootChatBot>(message);
                            if (chatBot != null)
                            {
                                _fileProcessing.SendAPILogInsertDBIC(chatBot, message, tenantid);
                            }
                        }

                        result = new ContentResult();
                        result.StatusCode = 200;
                        result.Content = hub_challenge.ToString();
                    }
                    catch (Exception ex)
                    {
                        new IboxLog("webhook-error:" + ex.Message, tenantid, "Error");
                        result = new ContentResult();
                        result.StatusCode = 500;
                        result.Content = ex.Message;
                    }
                }
                else
                {
                    result = new ContentResult();
                    result.StatusCode = 401;
                    result.Content = "Unauthorized";
                }
            }
            catch (Exception ex)
            {
                new IboxLog("Error ReceiveData: " + ex.Message, tenantid, "Error");
                result = new ContentResult();
                result.StatusCode = 500;
                result.Content = ex.Message;
            }
            return result;
        }

        /// <summary>
        /// Nhận thông tin KH (Cif, CifList, CustomerInfor)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ReceiveInfoCustomer/{tenantId}/{idBot}")]
        [ChatBotAuthorization]
        public HttpResponseMessage ReceiveInfoCustomer(object message, string tenantId, string idBot)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            try
            {
                new IboxLog("ReceiveInfoCustomer: " + message.ToString(), tenantId, "Info");
                InfoCustomer infoCustomer = JsonConvert.DeserializeObject<InfoCustomer>(message.ToString());
                if (infoCustomer == null)
                {
                    new IboxLog($"infoCustomer is null \n bodyRequest: {message.ToString()}", tenantId, "Error");
                    result.StatusCode = HttpStatusCode.BadGateway;
                    return result;
                }

                this.HttpContext.Response.CompleteAsync();

                _infoCustomerService.IdentificationCustomer(infoCustomer, tenantId, idBot);
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = HttpStatusCode.BadGateway;
                result.Content = new StringContent(ex.Message, Encoding.UTF8, "text/plain");
                new IboxLog("Error ReceiveInfoCustomer: " + ex.Message, tenantId, "Error");
                return result;
            }
        }

        [HttpPost]
        [HttpGet]
        [Route("CreateSessionIC/{tenantId}/{idBot}")]
        public HttpResponseMessage CreateSessionIC(CreateSessionIC message, string tenantId, string idBot)
        {
            HttpResponseMessage result = new HttpResponseMessage();
            try
            {
                CreateSessionIC message1 = message;
                this.HttpContext.Response.CompleteAsync();

                new IboxLog($"CreateSessionIC: {JsonConvert.SerializeObject(message1)}", tenantId, "Error");
                _infoCustomerService.CreateSessionIC(message1, tenantId, idBot);
                return result;
            }
            catch (Exception ex)
            {
                result.StatusCode = HttpStatusCode.BadGateway;
                result.Content = new StringContent(ex.Message, Encoding.UTF8, "text/plain");
                new IboxLog("Error CreateSessionIC: " + ex.Message, tenantId, "Error");
                return result;
            }
        }
    }
}