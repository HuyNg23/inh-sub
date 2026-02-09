using IBox.ChatBot.DB.Models.ChatBot;
using IBox.ChatBot.DB.Models.FPT;
using IBox.ChatBot.Service.BOT;
using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using WebAPI.Model.IC;
using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;

namespace IBox.ChatBot.Controllers
{
    [Route("api/ic")]
    [ApiController]
    [ICTokenAuthorization]
    public class ICController : Controller
    {
        private readonly IServiceBot _serviceBot;
        private readonly IBox.Common.Objects.IConfiguration _configuration;
        private readonly IRestAPI _restAPI;

        public ICController(IServiceBot serviceBot, Common.Objects.IConfiguration configuration, IRestAPI restAPI)
        {
            _serviceBot = serviceBot;
            _configuration = configuration;
            _restAPI = restAPI;
        }

        [HttpPost]
        [Route("UploadFile/{tenantid}")]
        public ReponeUploadfile? UploadFile(string tenantId, Uploadfile? obj)
        {
            var res = new ReponeUploadfile();
            try
            {
                new IboxLog("UploadFile IC Call:" + JsonConvert.SerializeObject(obj), tenantId, "Information");

                if (obj.file_url == "" || obj.app_id == "" || obj.type == "" || obj.username == "")
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Bad Request";
                }
                else
                {
                    res = _serviceBot.Upload_File(tenantId, obj);
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;

                new IboxLog($"ErrorPost UploadFile {ex.Message}", tenantId);
            }
            return res;
        }

        [HttpPost]
        [Route("SendMessage/{tenantid}")]
        public Ress SendMessage(string tenantid, SendMessage obj)
        {
            var res = new Ress();
            try
            {
                new IboxLog("SendMessage IC Call:" + JsonConvert.SerializeObject(obj), tenantid,"Info");
                if (obj.app_id == "" || obj.user_id_by_app == "" || obj.username == "" || obj.message == null)
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Bad Request";
                }
                else
                {
                    var userId = obj.user_id_by_app;
                    var splitUserId = userId.Split('_').ToList();
                    obj.user_id_by_app = splitUserId[splitUserId.Count - 1];
                    obj.channel = userId.Split('_')[0];
                    res = _serviceBot.Send_Message(tenantid, obj);
                    try
                    {
                        var dataIbox = new DB.Models.CreateFile.ChatBotMessage.RootChatBot()
                        {
                            timestamp = DateTimeOffset.UtcNow.AddHours(7).ToUnixTimeSeconds(),
                            bot_code = obj.app_id,
                            @event = "agent_chat",
                            data = new Data()
                            {
                                channel = obj.channel,
                                sender = new Sender()
                                {
                                    id = obj.user_id_by_app
                                },
                                message = new DB.Models.CreateFile.ChatBotMessage.Message()
                                {
                                    content = new DB.Models.CreateFile.ChatBotMessage.Content()
                                    {
                                        text = obj.message.text,
                                        url = !string.IsNullOrEmpty(obj.message.file_url) ? obj.message.file_id : ""
                                    },
                                    type = !string.IsNullOrEmpty(obj.message.file_url) ? "file" : "text"
                                }
                            }
                        };

                        RequestInsertDataChatBot requestInserDataChatBot = new RequestInsertDataChatBot()
                        {
                            bodyData = JsonConvert.SerializeObject(dataIbox),
                            senderId = obj.user_id_by_app,
                            tenantId = tenantid
                        };

                        var urlLogService = IBGlobalConfig.LogServiceIBox.FirstOrDefault(ptr => ptr.Contains(IBGlobalConfig.ThisSite));
                        _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(requestInserDataChatBot),
                            Headers = new List<RestAPIHeader>
                            {
                                  new RestAPIHeader()
                                  {
                                        Label = "Content-Type",
                                        Value = "application/json"
                                  }
                            },
                            Method = "POST",
                            Timeout = 30,
                            Url = $"{urlLogService}/api/SaveDataChatBot/SaveAgentChat/{tenantid}"
                        });
                    }
                    catch (Exception ex)
                    {
                        new IboxLog($"Agent support message: {ex.Message}", tenantid, "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                new IboxLog($"ErrorPost SendMessage {ex.Message}", tenantid, "Error");
            }
            return res;
        }

        [HttpPost]
        [Route("DisableBot/{tenantid}")]
        public Ress DisableBot(string tenantid, GetListHistory obj)
        {
            var res = new Ress();
            try
            {
                new IboxLog("DisableBot IC Call:" + JsonConvert.SerializeObject(obj), tenantid, "Info");
                if (obj.app_id == "" || obj.user_id_by_app == "" || obj.username == "")
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Bad Request";
                }
                else
                {
                    var userId = obj.user_id_by_app;
                    obj.user_id_by_app = userId.Split('_')[1];
                    obj.channel = userId.Split('_')[0];
                    res = _serviceBot.Disable_Bot(tenantid, obj);
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                new IboxLog($"ErrorPost DisableBot {ex.Message}", tenantid, "Error");
            }
            return res;
        }

        [HttpPost]
        [Route("EnableBot/{tenantid}")]
        public Ress EnableBot(string tenantid, GetListHistory obj)
        {
            var res = new Ress();
            try
            {
                new IboxLog("EnableBot IC Call:" + JsonConvert.SerializeObject(obj), tenantid, "Info");
                if (obj.user_id_by_app == "")
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Bad Request";
                }
                else
                {
                    var userId = obj.user_id_by_app;
                    obj.user_id_by_app = userId.Split('_')[1];
                    obj.channel = userId.Split('_')[0];
                    res = _serviceBot.Enable_Bot(tenantid, obj);
                    _serviceBot.EndChatIC(tenantid, res, obj);
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                new IboxLog($"ErrorPost EnableBot {ex.Message}", tenantid, "Error");
            }
            return res;
        }
    }
}