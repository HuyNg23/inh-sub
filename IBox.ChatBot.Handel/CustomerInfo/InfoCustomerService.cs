using IBox.ChatBot.DB.Models.FPT;
using IBox.ChatBot.DB.Models.IC;
using IBox.Common.ConsistentHashing;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.DLEx.Execution;
using Newtonsoft.Json;
using Serilog;
using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;

namespace IBox.ChatBot.Service.CustomerInfo
{
    public class InfoCustomerService : IInfoCustomerService
    {
        private readonly IRestAPI _restAPI;

        public InfoCustomerService(IRestAPI restAPI)
        {
            _restAPI = restAPI;
        }

        public void IdentificationCustomer(InfoCustomer infoCustomer, string tenantId, string idBot)
        {
            try
            {
                var lstServiceLog = ConsistentHashing.GetServers(infoCustomer.sender_id);
                foreach (var urlLogService in lstServiceLog)
                {
                    try
                    {
                        _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(infoCustomer),
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
                            Url = $"{urlLogService}/api/SaveDataChatBot/IdentificationCustomer/{tenantId}"
                        });

                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call api IdentificationCustomer: {ex.Message} \n infoCustomer: {JsonConvert.SerializeObject(infoCustomer)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"IdentificationCustomer: {ex.Message}");
                throw;
            }
        }

        public void CreateSessionIC(CreateSessionIC createSessionIC, string tenantId, string idBot)
        {
            try
            {
                if (createSessionIC == null)
                {
                    Log.Error("infoCustomer is null");
                    return;
                }

                Thread.Sleep(1000);
                //Call api đến các server để thực hiện gửi tin nhắn
                foreach (var urlLogService in IBGlobalConfig.LogServiceIBox)
                {
                    try
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
                            Timeout = 10,
                            Url = $"{urlLogService}/api/SaveDataChatBot/CreateSessionIC/{tenantId}/{createSessionIC?.data?.user_social_id ?? "123"}/{createSessionIC?.data?.customer_id ?? "123"}/{createSessionIC?.data?.id ?? "123"}"
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call api CreateSessionIC sang cac server {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateSessionIC: {ex.Message}");
            }
        }
    }
}