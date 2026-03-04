using IBox.ChatBot.DB.DBChatDay;
using IBox.ChatBot.DB.Models.ChatBot;
using IBox.ChatBot.DB.Models.CreateFile.IBox;
using IBox.ChatBot.DB.Models.CRM;
using IBox.ChatBot.DB.Models.FPT;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;

namespace IBox.LogService.Controller.ChatBot
{
    [Route("api/[controller]")]
    [ApiController]
    public class SaveDataChatBotController : ControllerBase
    {
        private readonly IChangeDBChatDay _changeDBChatDay;
        private readonly IRestAPI _restAPI;
        private readonly ICommonData _commonData;

        public SaveDataChatBotController(IChangeDBChatDay changeDBChatDay, ICommonData commonData, IRestAPI restAPI)
        {
            _changeDBChatDay = changeDBChatDay;
            _commonData = commonData;
            _restAPI = restAPI;
        }

        [HttpPost]
        [Route("SaveFileChatBot")]
        public void SaveFileChatBot(RequestInsertDataChatBot? requestInsertDataChatBot)
        {
            try
            {
                if (requestInsertDataChatBot == null)
                {
                    return;
                }

                var sessionObj = SessionChatBot.GetSession(requestInsertDataChatBot.senderId);
                if (sessionObj != null)
                {
                    requestInsertDataChatBot.sessionId = sessionObj.SessionId;
                }

                var session = SessionChatBot.GetSession(requestInsertDataChatBot.senderId);
                if (session != null && session.IsSupport)
                {
                    requestInsertDataChatBot.isInputIC = true;
                    _changeDBChatDay.InputICIsSupport(requestInsertDataChatBot);
                }

                string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolder, requestInsertDataChatBot.tenantId);
                _commonData.CreateFolder1(path);
                int valueSecond = _commonData.ShowSecond();
                for (int i = 0; i < 30; i++)
                {
                    string filePath = Path.Combine(path, $"{DateTime.Now.ToString("yyyyMMddHHmm")}{valueSecond}_{i}.wal");
                    try
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                fs.Seek(0, SeekOrigin.End);
                                sw.WriteLine($"{JsonConvert.SerializeObject(requestInsertDataChatBot)},");
                            }
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error SaveFile: {ex.Message}");
            }
        }

        [HttpPost]
        [Route("CheckSessionDB/{tenantId}/{sessionId}")]
        public object CheckSessionDB(string tenantId, string sessionId, ChatSessionDaily CheckSession)
        {
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChat == null)
                {
                    Log.Error($"CheckSessionDB configChat is null  tenantId: {tenantId}");
                    return new
                    {
                        SessionId = ""
                    };
                }

                var sessionIdDay = _changeDBChatDay.CheckSessionDB(configChat, tenantId, sessionId, CheckSession);
                return new
                {
                    SessionId = sessionIdDay
                };
            }
            catch (Exception ex)
            {
                Log.Error($"Error CheckSession: {ex.Message} \n tenantId: {tenantId} sessionId: {sessionId} CheckSession: {JsonConvert.SerializeObject(CheckSession)}");
                return new
                {
                    SessionId = ""
                };
            }
        }

        [HttpGet]
        [Route("GetDataChatBotSenderId/{tenantId}/{senderId}")]
        public object GetDataChatBotSenderId(string tenantId, string senderId)
        {
            try
            {
                return _changeDBChatDay.GetDataChatBotSenderId(tenantId, senderId);
            }
            catch (Exception ex)
            {
                Log.Error($"Error CheckSession: {ex.Message} \n tenantId: {tenantId} senderId: {senderId}");
                return new List<ResponseChatBotGetSenderId>();
            }
        }

        [HttpGet]
        [Route("CheckSessionRequestSupport")]
        public void CheckSessionRequestSupport(RequestCheckSupport requestCheckSupport)
        {
            var chatBot = JsonConvert.DeserializeObject<IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage.RootChatBot>(requestCheckSupport.bodyDataSupport);
            var respChatDaySession = JsonConvert.DeserializeObject<ResponseChatDaySession>(requestCheckSupport.chatDaySession);

            if (chatBot == null || respChatDaySession == null)
            {
                Log.Error($"requestCheckSupport: {JsonConvert.SerializeObject(requestCheckSupport)}");
                return;
            }

            _changeDBChatDay.CheckSessionRequestSupport(chatBot, requestCheckSupport.tenantId, respChatDaySession);
        }

        [HttpGet]
        [HttpPost]
        [Route("EndChatIC/{tenantId}/{senderId}")]
        public void EndChatIC(string tenantId, string senderId)
        {
            var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
            if (configChat == null)
            {
                Log.Error($"EndChatIC configChat is null tenantId: {tenantId}");
                return;
            }

            _changeDBChatDay.RemoveSessionId(senderId);
            _changeDBChatDay.EndChatIC(configChat, tenantId, senderId);
        }

        [HttpPost]
        [HttpGet]
        [Route("RemoveSessionId/{senderId}")]
        public void RemoveSessionId(string senderId)
        {
            _changeDBChatDay.RemoveSessionId(senderId);
        }

        [HttpGet]
        [Route("GetDataChatBot/{senderId}/{tenantId}/{sessionId}/{dateTimeCurrent}")]
        public ListDataIBox GetDataChatBot(string senderId, string tenantId, string sessionId, string dateTimeCurrent)
        {
            return _changeDBChatDay.GetDataChatBot(senderId, tenantId, sessionId, dateTimeCurrent);
        }

        /// <summary>
        /// Tự động EndChat
        /// </summary>
        [HttpGet]
        [Route("EndChatAll")]
        public void EndChatAll()
        {
            _changeDBChatDay.EndChatAll();
        }

        /// <summary>
        /// Zip file chat bot theo lịch (không zip file 3 ngày gần nhất)
        /// </summary>
        [HttpGet]
        [Route("ZipFileChatBot")]
        public void ZipFileChatBot()
        {
            _changeDBChatDay.ZipFileChatBot();
        }

        /// <summary>
        /// Khi IC tạo phiên chát gửi API đến ChatBot đẩy toàn bộ tin nhắn đồng thời định danh KH trên IC
        /// Và đẩy data Update truyền ContactId vào Interaction CRM
        /// Và đẩy data Update IC truyền ContactId và InteractionId
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="senderId"></param>
        /// <param name="customerId"></param>
        /// <param name="idic"></param>
        [HttpGet]
        [Route("CreateSessionIC/{tenantId}/{user_social_id}/{customerId}/{idic}")]
        public void CreateSessionIC(string tenantId, string user_social_id, string customerId, string idic)
        {
            string tenantId1 = tenantId;
            string user_social_id1 = user_social_id;
            string customerId1 = customerId;
            string idic1 = idic;

            HttpContext.Response.CompleteAsync();
            Log.Information($"CreateSessionIC tenantId: {tenantId1}, user_social_id: {user_social_id1}, customerId: {customerId1}, idic: {idic1}, Thisite: {IBGlobalConfig.ThisSite}");
            _changeDBChatDay.CreateSessionIC(tenantId1, user_social_id1, customerId1, idic1);
        }

        [HttpGet]
        [HttpPost]
        [Route("IdentificationCustomer/{tenantId}")]
        public void IdentificationCustomer(InfoCustomer infoCustomer, string tenantId)
        {
            try
            {
                if (infoCustomer == null)
                {
                    return;
                }

                string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderCustomer, tenantId);
                _commonData.CreateFolder1(path);
                int valueSecond = _commonData.ShowSecond();
                for (int i = 0; i < 30; i++)
                {
                    string filePath = Path.Combine(path, $"{DateTime.Now.ToString("yyyyMMddHHmm")}_{valueSecond}_{i}.wal");
                    try
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        {
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                fs.Seek(0, SeekOrigin.End);
                                sw.WriteLine($"{JsonConvert.SerializeObject(infoCustomer)},");
                            }
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error Save IdentificationCustomer: {ex.Message} \n infoCustomer: {JsonConvert.SerializeObject(infoCustomer)}");
            }
        }

        [HttpPost]
        [Route("SaveAgentChat/{tenantId}")]
        public void SaveAgentChat(RequestInsertDataChatBot requestInserDataChatBot, string tenantId)
        {
            var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);

            if (configChat == null)
            {
                return;
            }

            SessionObj sessionObj = CheckSession(requestInserDataChatBot.senderId);
            ResponseChatDaySession responseChatDaySession = new ResponseChatDaySession()
            {
                IsRequestSupport = false,
                SessionId = sessionObj.SessionId
            };

            _changeDBChatDay.ChangeDBChatDayChatMessageDaily(configChat, responseChatDaySession, requestInserDataChatBot, true);
        }

        [HttpGet]
        [Route("UpdateThisSiteIdentification/{tenantId}/{senderId}/{thisSite}")]
        public void UpdateThisSiteIdentification(string tenantId, string senderId, string thisSite)
        {
            try
            {
                HttpContext.Response.CompleteAsync();
                var session = SessionChatBot.GetSession(senderId);
                if (session != null)
                {
                    _changeDBChatDay.CheckAndCreateSession(tenantId, senderId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateThisSiteIdentification {ex.Message}\n senderId: {senderId}, thisSite: {thisSite}");
            }
        }

        [HttpGet]
        [Route("AddSession/{tenantId}/{senderId}")]
        public SessionObj AddSession(string tenantId, string senderId)
        {
            try
            {
                return _changeDBChatDay.CheckAndCreateSession(tenantId, senderId);
            }
            catch (Exception ex)
            {
                Log.Error($"Add Session {ex.Message}");
                return new SessionObj();
            }
        }

        [HttpGet]
        [Route("CheckSession/{senderId}")]
        public SessionObj CheckSession(string senderId)
        {
            try
            {

                return SessionChatBot.GetSession(senderId);

            }
            catch (Exception ex)
            {
                Log.Error($"Add Session {ex.Message}");
                return new SessionObj();
            }
        }

        [HttpGet]
        [Route("CheckAndGenSession/{senderId}")]
        public SessionObj? CheckAndGenSession(string senderId)
        {
            var sessionObj = SessionChatBot.GetSession(senderId);
            //if (sessionObj == null || string.IsNullOrEmpty(sessionObj.SessionId))
            //{
            //    string sessionIdGuid = Guid.NewGuid().ToString();
            //    sessionObj = new SessionObj()
            //    {
            //        SessionId = sessionIdGuid,
            //        ThisSite = "",
            //        CreateDate = DateTime.Now
            //    };

            //    SessionChatBot.AddOrUpdateSession(senderId, sessionObj);
            //}

            return sessionObj;
        }

        [HttpGet]
        [Route("GetSession/{senderId}")]
        public SessionObj? GetSession(string senderId)
        {
            try
            {
                return SessionChatBot.GetSession(senderId);
            }
            catch (Exception ex)
            {
                Log.Error($"Add Session {ex.Message}");
                return new SessionObj();
            }
        }

        [HttpPost]
        [Route("InputInteractionCRM/{tenantId}")]
        public IActionResult InputInteractionCRM(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId)
        {
            try
            {
                RequestUpdateInteractionChatBot requestUpdateInteractionChatBot1 = requestUpdateInteractionChatBot;
                Log.Information($"requestUpdateInteractionChatBot1 : {JsonConvert.SerializeObject(requestUpdateInteractionChatBot1)}");
                _changeDBChatDay.UpdateInteractionChat(requestUpdateInteractionChatBot1, tenantId);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"InputInteractionCRM {ex.Message} \n requestUpdateInteractionChatBot: {requestUpdateInteractionChatBot}, tenantId:{tenantId}");
                return StatusCode(500, $"{ex.Message}");
            }
        }

        [HttpPost]
        [Route("UpdateInteractionChatSub/{tenantId}")]
        public IActionResult UpdateInteractionChatSub(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId)
        {
            try
            {
                _changeDBChatDay.UpdateInteractionChatSub(requestUpdateInteractionChatBot, tenantId);
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateInteractionChatSub {ex.Message}");
                return StatusCode(500, $"{ex.Message}");
            }
        }

        [HttpPost]
        [Route("InputContactCRM/{tenantId}")]
        public void InputContactCRM(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(requestUpdateContactChatBot.SenderId))
                {
                    return;
                }

                _changeDBChatDay.UpdateContactChat(requestUpdateContactChatBot, tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"InputInteractionCRM {ex.Message}");
            }
        }

        [HttpPost]
        [Route("UpdateContactChatSub/{tenantId}")]
        public void UpdateContactChatSub(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId)
        {
            try
            {
                _changeDBChatDay.UpdateContactChatSub(requestUpdateContactChatBot, tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateContactChatSub {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetChatSessionDaily/{tenantId}/{senderId}")]
        public ChatSessionDaily? GetChatSessionDaily(string tenantId, string senderId)
        {
            try
            {
                return _changeDBChatDay.GetChatSessionDaily(tenantId, senderId);
            }
            catch (Exception ex)
            {
                Log.Error($"GetChatSessionDaily {ex.Message} \n tenantId: {tenantId}, senderId: {senderId}");
                throw;
            }
        }

        [HttpPost]
        [Route("InsertOrUpdateCustomerChat/{tenantId}/{senderId}")]
        public void InsertOrUpdateCustomerChat(InfoCustomer infoCustomer, string tenantId, string senderId)
        {
            try
            {
                _changeDBChatDay.InsertOrUpdateCustomerChat(infoCustomer, tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"InsertOrUpdateCustomerChat {ex.Message} \n tenantId: {tenantId}, senderId: {senderId}");
            }
        }
    }
}