using IBox.ChatBot.DB.Models.FPT;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using Newtonsoft.Json;
using Serilog;
using WebAPI.Model.IC;

namespace IBox.ChatBot.Service.BOT
{
    public class ServiceBot : IServiceBot
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly ICommonData _commonData;
        private readonly IRestAPI _restAPI;

        public ServiceBot(Common.Objects.IConfiguration configuration, ICommonData commonData, IRestAPI restAPI)
        {
            _configuration = configuration;
            _commonData = commonData;
            _restAPI = restAPI;
        }

        public Ress Send_Message(string tenantid, SendMessage sms)
        {
            var res = new Ress();
            string file_name = "";
            var configChatBot = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid && x.IdBOT == sms.app_id);
            if (configChatBot == null)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = "config is null";
                Log.Error($"Send_Message: config is null \n body: {JsonConvert.SerializeObject(sms)}");
                return res;
            }

            if (sms == null)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = "config is null";
                Log.Error($"Send_Message: sms is null");
                return res;
            }

            string url = configChatBot.Url_Api_BOT_SendMessage;
            string body = "";
            try
            {
                string TokenBOT = configChatBot.ListToken;
                ListJson ListModel = new ListJson();

                if (!string.IsNullOrEmpty(sms.message.file_url))
                {
                    if (string.IsNullOrEmpty(sms.message.text))
                    {
                        file_name = GetFileName(sms);
                        SendMessageUploadModel sendMessageModel1 = new SendMessageUploadModel();
                        sendMessageModel1.channel = sms.channel;
                        sendMessageModel1.sender_id = sms.user_id_by_app;
                        sendMessageModel1.message = new DB.Models.FPT.Message1();
                        sendMessageModel1.message.type = "file";
                        sendMessageModel1.message.content = new Content1();
                        sendMessageModel1.message.content.url = sms.message.file_id;
                        sendMessageModel1.message.content.file_name = file_name.Split('.')[0];
                        body = JsonConvert.SerializeObject(sendMessageModel1);
                    }
                    else
                    {
                        file_name = GetFileName(sms);
                        SendMessageUploadModel sendMessageModel1 = new SendMessageUploadModel();
                        sendMessageModel1.channel = sms.channel;
                        sendMessageModel1.sender_id = sms.user_id_by_app;
                        sendMessageModel1.message = new DB.Models.FPT.Message1();
                        sendMessageModel1.message.type = "file";
                        sendMessageModel1.message.content = new Content1();
                        sendMessageModel1.message.content.url = sms.message.file_id;
                        sendMessageModel1.message.content.file_name = file_name.Split('.')[0];
                        body = JsonConvert.SerializeObject(sendMessageModel1);
                        HttpResponseMessage response1 = _commonData.ExecuteAPI(body, url, TokenBOT);

                        if (response1.IsSuccessStatusCode)
                        {
                            string strResponse = response1.Content.ReadAsStringAsync().Result;
                        }

                        SendMessageModel sendMessageModel = new SendMessageModel();
                        sendMessageModel.channel = sms.channel;
                        sendMessageModel.sender_id = sms.user_id_by_app;
                        sendMessageModel.message = new DB.Models.FPT.Message();
                        sendMessageModel.message.type = "text";
                        sendMessageModel.message.content = new DB.Models.FPT.Content();
                        sendMessageModel.message.content.text = sms.message.text;
                        body = JsonConvert.SerializeObject(sendMessageModel);
                    }
                }
                else
                {
                    SendMessageModel sendMessageModel = new SendMessageModel();
                    sendMessageModel.channel = sms.channel;
                    sendMessageModel.sender_id = sms.user_id_by_app;
                    sendMessageModel.message = new DB.Models.FPT.Message();
                    sendMessageModel.message.type = "text";
                    sendMessageModel.message.content = new DB.Models.FPT.Content();
                    sendMessageModel.message.content.text = sms.message.text;
                    body = JsonConvert.SerializeObject(sendMessageModel);
                }


                HttpResponseMessage response = _commonData.ExecuteAPI(body, url, TokenBOT);
                if (response.IsSuccessStatusCode)
                {
                    Log.Debug($"response SendMessage: {response}");
                    res.statecode = "200";
                    res.error = 0;
                    res.message = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Log.Debug($"response SendMessage: {response},response== null");
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Không có dữ liệu trả về !";
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                Log.Error($"Error SendBot" +
                    $"\n{ex.Message}");
            }
            return res;
        }

        public ReponeUploadfile Upload_File(string tenantid, Uploadfile sms)
        {
            var res = new ReponeUploadfile();
            try
            {
                var configChatBot = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid && x.IdBOT == sms.app_id);
                if (configChatBot == null)
                {
                    res.statecode = "08";
                    res.error = 1;
                    res.message = "config chat is null";
                    Log.Error($"Upload file config is null: {JsonConvert.SerializeObject(sms)}");
                    return res;
                }

                string url = configChatBot.Url_Api_BOT_UploadFile;
                string TokenBOT = configChatBot.ListToken;

                string[] s = _commonData.SplitFolder(sms.file_url);

                string file_name = s[s.Length - 1];
                var response = _commonData.UploadFileToFPT(url, _commonData.ConvertUrlToByte(sms.file_url), file_name, TokenBOT);
                if (response != null && response.ok == "true")
                {
                    Log.Debug($"response Upload_File: {JsonConvert.SerializeObject(response)}");
                    res.statecode = "200";
                    res.error = 0;
                    res.message = "Success";
                    res.data = new data1();
                    res.data.file_id = response.url;
                }
                else
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Không có dữ liệu trả về !";
                    Log.Debug($"response Upload_File: {JsonConvert.SerializeObject(response)},response== null");
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                Log.Error($"Error Upload_File" +
                $"\n{ex.Message}");
            }
            return res;
        }

        public Ress Disable_Bot(string tenantid, GetListHistory sms)
        {
            var res = new Ress();
            var configChatBot = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid && x.IdBOT == sms.app_id);
            if (configChatBot == null)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = "config is null";
                Log.Error($"Disable_Bot: config is null \n body: {JsonConvert.SerializeObject(sms)}");
                return res;
            }

            DisableBotModel disableBotModel = new DisableBotModel();
            disableBotModel.sender_id = sms.user_id_by_app;
            disableBotModel.channel = sms.channel;
            string TokenBOT = configChatBot.ListToken;
            string msg1 = JsonConvert.SerializeObject(disableBotModel);
            string url = configChatBot.Url_Api_BOT_DisableBot;
            try
            {
                HttpResponseMessage response = _commonData.ExecuteAPI(msg1, url, TokenBOT);
                if (response.IsSuccessStatusCode)
                {
                    Log.Debug($"response Disable_Bot: {response}");
                    res.statecode = "200";
                    res.error = 0;
                    res.message = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    res.statecode = "02";
                    res.error = 1;
                    res.message = "Không có dữ liệu trả về !";
                    Log.Debug($"response Disable_Bot: response = null");
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                Log.Error($"Error Disable_Bot" +
                $"\n{ex.Message}");
            }
            return res;
        }

        public Ress Enable_Bot(string tenantid, GetListHistory sms)
        {
            var res = new Ress();
            var configChatBot = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(x => x.tenant_id == tenantid && x.IdBOT == sms.app_id);
            if (configChatBot == null)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = "config is null";
                Log.Error($"Enable_Bot: config is null \n body: {JsonConvert.SerializeObject(sms)}");
                return res;
            }

            EnableBotModel enableBotModel = new EnableBotModel();
            enableBotModel.sender_id = sms.user_id_by_app;
            enableBotModel.channel = sms.channel;
            string TokenBOT = configChatBot.ListToken;
            enableBotModel.description = "Support done";
            string msg1 = JsonConvert.SerializeObject(enableBotModel);
            string url = configChatBot.Url_Api_BOT_EnableBot;
            try
            {
                HttpResponseMessage response = _commonData.ExecuteAPI(msg1, url, TokenBOT);
                //bật bot để chuyển sang bot tiếp tục chat tự động với khách hàng
                if (response.IsSuccessStatusCode)
                {
                    Log.Debug($"response Enable_Bot: Done: {response}");
                    res.statecode = "200";
                    res.error = 0;
                    res.message = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    Log.Debug($"response Enable_Bot: response== null");
                    res.statecode = ((int)response.StatusCode).ToString();
                    res.error = 1;
                    res.message = "Không có dữ liệu trả về !";
                }
            }
            catch (Exception ex)
            {
                res.statecode = "08";
                res.error = 1;
                res.message = ex.Message;
                Log.Error($"Error Enable_Bot" +
                $"\n{ex.Message}");
            }
            return res;
        }

        public string GetFileName(SendMessage sms)
        {
            string[] s;
            string file_name = "";
            try
            {

                if (sms.message.file_id != "")
                {

                    if (sms.message.file_id.Contains('/'))
                    {
                        s = sms.message.file_id.Split('/');
                        file_name = s[s.Length - 1];
                    }
                    else
                    {
                        s = sms.message.file_id.Split(Path.DirectorySeparatorChar);
                        file_name = s[s.Length - 1];
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Error("Error Split get file name: " + ex.Message);
                file_name = "";
            }
            return file_name;
        }

        public void EndChatIC(string tenantid, Ress res, GetListHistory obj)
        {
            try
            {
                if (res.statecode == "200")
                {
                    //Call API kết thúc phiên chat đến tất cả server
                    try
                    {
                        foreach (var urLogService in IBGlobalConfig.LogServiceIBox)
                        {
                            var resultSenderId = _restAPI.SendChatBot(new RestAPIRequest()
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
                                Url = $"{urLogService}/api/SaveDataChatBot/EndChatIC/{tenantid}/{obj.user_id_by_app}"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call API End Chat theo senderId: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatIC: {ex.Message} \n Ress {JsonConvert.SerializeObject(res)}, GetListHistory {JsonConvert.SerializeObject(obj)}");
            }
        }
    }
}