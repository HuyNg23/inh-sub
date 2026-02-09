using IBox.ChatBot.DB.Models.ChatBot;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.ConsistentHashing;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx.Execution;
using Newtonsoft.Json;
using Serilog;
using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;

namespace IBox.ChatBot.Service
{
    public class FileProcessing : IFileProcessing
    {
        public readonly ICommonData _commonData;
        public readonly Common.Objects.IConfiguration _configuration;
        public readonly IBContext<TenantContext> _tenantContext;
        public readonly ICallData _callData;
        public readonly IExecuteWF _executeWF;
        public readonly IEncryption _encryption;
        public readonly IRestAPI _restAPI;

        public FileProcessing(ICommonData commonData, ICallData callData, Common.Objects.IConfiguration configuration, IBContext<TenantContext> tenantContext, IExecuteWF executeWF, IEncryption encryption, IRestAPI restAPI)
        {
            _commonData = commonData;
            _callData = callData;
            _configuration = configuration;
            _tenantContext = tenantContext;
            _executeWF = executeWF;
            _encryption = encryption;
            _restAPI = restAPI;
        }

        public void SendAPILogInsertDBIC(RootChatBot rootChatBot, string bodyData, string tenantId)
        {
            try
            {
                if (rootChatBot == null || rootChatBot.@event == null)
                {
                    return;
                }

                bool isSupport = false;
                var lstServiceLog = ConsistentHashing.GetServers(rootChatBot.data?.sender?.id);

                if (rootChatBot.@event.Contains("user_request_support"))
                {
                    isSupport = true;
                    foreach (var ipService in lstServiceLog)
                    {
                        _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = "{}",
                            Headers = new List<RestAPIHeader>()
                            {
                              new RestAPIHeader()
                              {
                                  Label = "Content-Type",
                                  Value = "application/json"
                              }
                            },
                            Method = "GET",
                            Timeout = 30,
                            Url = $"{ipService}/api/SaveDataChatBot/AddSession/{tenantId}/{rootChatBot.data?.sender?.id}"
                        });
                    }
                }

                var body = new DataFile()
                {
                    bodyData = bodyData,
                    bot_code = rootChatBot.bot_code,
                    channel = rootChatBot.data?.channel,
                    isInputIC = false,
                    isSupport = isSupport,
                    name = rootChatBot.data?.sender?.name,
                    sub_channel = rootChatBot.data?.sub_channel,
                    tenantId = tenantId,
                    senderId = rootChatBot.data?.sender?.id,
                };

                foreach (var ipService in lstServiceLog)
                {
                    try
                    {
                        _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(body),
                            Headers = new List<RestAPIHeader>()
                                {
                                     new RestAPIHeader()
                                     {
                                          Label = "Content-Type",
                                          Value = "application/json"
                                     }
                                },
                            Method = "POST",
                            Timeout = 30,
                            Url = $"{ipService}/api/SaveDataChatBot/SaveFileChatBot",
                        });

                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call api SaveFileChatBot: {ex.Message}");
                    }
                }

                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChat == null)
                {
                    return;
                }

                if (rootChatBot.@event.Contains("mark_as_done"))
                {
                    //Call api để ngắt session
                    foreach (var urlLog in lstServiceLog)
                    {
                        _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = "{}",
                            Headers = new List<RestAPIHeader>()
                            {
                                  new RestAPIHeader()
                                  {
                                       Label = "Content-Type",
                                       Value = "application/json"
                                  }
                            },
                            Method = "GET",
                            Timeout = 30,
                            Url = $"{urlLog}/api/SaveDataChatBot/RemoveSessionId/{rootChatBot.data?.sender?.id ?? ""}"
                        });
                    }

                    try
                    {
                        var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                        var tenantContext = tContext.GetTenantContext(configChat.tenant_id).Context;
                        this._executeWF.SetTenantContext(tenantContext);
                        var result = _executeWF.Execute(configChat.Ev_EndChat, JsonConvert.SerializeObject(rootChatBot), tenantId);
                        tenantContext.Context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"mark_as_done: {ex.Message}");
                    }
                }
                else if (rootChatBot.@event.Contains("predict"))
                {
                    try
                    {
                        var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                        var tenantContext = tContext.GetTenantContext(configChat.tenant_id).Context;
                        this._executeWF.SetTenantContext(tenantContext);
                        var result = _executeWF.Execute(configChat.Ev_Predict, JsonConvert.SerializeObject(rootChatBot), tenantId);
                        tenantContext.Context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"predict: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendAPILogInsertDBIC: {ex.Message} \n senderId: {rootChatBot?.data?.sender?.id}, bodyData: {bodyData}");
            }
        }
    }
}