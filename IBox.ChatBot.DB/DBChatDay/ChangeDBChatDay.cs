using IBox.ChatBot.DB.Models.ChatBot;
using IBox.ChatBot.DB.Models.CreateFile.CRM.Respone;
using IBox.ChatBot.DB.Models.CreateFile.IBox;
using IBox.ChatBot.DB.Models.CreateFile;
using IBox.ChatBot.DB.Models.CRM;
using IBox.ChatBot.DB.Models.FPT;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.DLEx.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System.Data;
using System.IO.Compression;
using IBox.Common.ConsistentHashing;
using IBox.ChatBot.HandleScheduleSQL;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;
using DocumentFormat.OpenXml.Bibliography;
using IBox.Database.Root.Tables;
using Renci.SshNet;
using IBox.Database.ChatBot;
using System.Text;
using WebAPI.Model.IC;
using System.Reflection;
using DocumentFormat.OpenXml.Spreadsheet;
using MongoDB.Driver.Core.Servers;

namespace IBox.ChatBot.DB.DBChatDay
{
    public class ChangeDBChatDay : IChangeDBChatDay
    {
        private readonly DBChatDayContextFactory _contextFactory;
        public readonly IRestAPI _restAPI;
        public readonly IConfiguration _configuration;
        public readonly IEncryption _encryption;
        public readonly IExecuteWF _executeWF;
        public readonly ICommonData _commonData;
        public readonly ICreateDB _createDB;


        public ChangeDBChatDay(IRestAPI restAPI, IConfiguration configuration, IEncryption encryption, IExecuteWF executeWF, DBChatDayContextFactory contextFactory, ICommonData commonData, ICreateDB iCreateDB)
        {
            _restAPI = restAPI;
            _configuration = configuration;
            _encryption = encryption;
            _executeWF = executeWF;
            _contextFactory = contextFactory;
            _commonData = commonData;
            _createDB = iCreateDB;
        }

        public string ChangeDBChatDayCustomer(CB_ConfigChat configChat, string? senderId, int checkCount = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    new IboxLog("requestInserDataChatBot is null", configChat.tenant_id, "Error");
                    return "";
                }

                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(configChat.tenant_id).Context;

                var customer = tenantContext.Context.CB_Customers.FirstOrDefault(x => x.SenderId == senderId);
                if (customer == null)
                {
                    tenantContext.Context.CB_Customers.Add(new CB_Customer()
                    {
                        SenderId = senderId
                    });
                    tenantContext.Context.SaveChanges();
                }

                return senderId;
            }
            catch (Exception ex)
            {
                new IboxLog($"ChangeDBChatDayCustomer: {ex.Message} \n senderId: {senderId} checkCount: {checkCount}", configChat.tenant_id, "Error");
                if (checkCount < Int32.Parse(configChat.Retry))
                {
                    return ChangeDBChatDayCustomer(configChat, senderId, checkCount++);
                }
                return "";
            }
        }

        public string? CheckSessionDB(CB_ConfigChat configChat, string tenantId, string sessionId, ChatSessionDaily CheckSession, int checkCount = 0)
        {
            try
            {
                string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, DateTime.Now);
                string PathdbDayNow = Path.Combine(filePathcurrent, $"chatbot.db");

                ChatSessionDaily? chatSessionDayNow = new ChatSessionDaily();
                using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow))
                {
                    chatSessionDayNow = dbContextDaily.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SessionId == sessionId && ptr.IsClose == false);

                    if (chatSessionDayNow == null)
                    {
                        dbContextDaily.Context.ChatSessionDailies.Add(CheckSession);
                        dbContextDaily.Context.SaveChangesAsync();
                    }
                    else
                    {
                        return chatSessionDayNow.SessionId;
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"CheckSessionDB: {ex.Message}", configChat.tenant_id, "Error");
                if (checkCount < Int32.Parse(configChat.Retry))
                {
                    return CheckSessionDB(configChat, tenantId, sessionId, CheckSession, checkCount++);
                }
            }
            return "";
        }

        public void ChangeDBChatDayChatMessageDaily(CB_ConfigChat configChat, ResponseChatDaySession responseChatDaySession, RequestInsertDataChatBot requestInserDataChatBot, bool isAgentChat = false, int checkCount = 0)
        {
            try
            {
                if (requestInserDataChatBot == null || string.IsNullOrEmpty(requestInserDataChatBot.bodyData) || string.IsNullOrEmpty(responseChatDaySession.SessionId) || string.IsNullOrEmpty(requestInserDataChatBot.tenantId))
                {
                    new IboxLog($"Error ChangeDBChatDayChatMessageDaily is null sessionId {JsonConvert.SerializeObject(responseChatDaySession)} \n requestInserDataChatBot: {JsonConvert.SerializeObject(requestInserDataChatBot)}", configChat.tenant_id, "Error");
                    return;
                }

                if (responseChatDaySession.IsRequestSupport == true)
                {
                    try
                    {
                        var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                        var tenantContext = tContext.GetTenantContext(requestInserDataChatBot.tenantId).Context;
                        this._executeWF.SetTenantContext(tenantContext);
                        var result = _executeWF.Execute(configChat.WF_SendChatIC, requestInserDataChatBot.bodyData, requestInserDataChatBot.tenantId);
                        tenantContext.Context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        new IboxLog($"InputIC: {ex.Message}", configChat.tenant_id, "Error");
                    }
                }

                if (string.IsNullOrEmpty(responseChatDaySession.SessionId))
                {
                    int day = 0;
                    while (day < 3)
                    {
                        string basePath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, requestInserDataChatBot.tenantId);
                        string filePath = _commonData.GetFilePath(basePath, DateTime.Now.AddDays(-day));
                        string pathYesterday = Path.Combine(filePath, $"chatbot.db");
                        if (!File.Exists(pathYesterday))
                        {
                            day++;
                            continue;
                        }

                        using (var dbContextDaily = _contextFactory.CreateContext(pathYesterday))
                        {
                            var chatSessionDaily = dbContextDaily.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SenderId == requestInserDataChatBot.senderId && ptr.IsClose == false);
                            if (chatSessionDaily != null)
                            {
                                responseChatDaySession.SessionId = chatSessionDaily.SessionId;
                                break;
                            }
                        }
                        day++;
                    }
                }

                string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, requestInserDataChatBot.tenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, DateTime.Now);
                string PathdbDayNow = Path.Combine(filePathcurrent, $"chatbot.db");
                using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow))
                {
                    dbContextDaily.Context.ChatMessageDailies.Add(new ChatMessageDaily()
                    {
                        MessageContent = requestInserDataChatBot.bodyData,
                        SessionId = responseChatDaySession.SessionId,
                        IsInputIC = isAgentChat == true ? true : responseChatDaySession.IsRequestSupport
                    });
                    dbContextDaily.Context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"Error ChangeDBChatDayChatMessageDaily {ex.Message} \n sessionId {JsonConvert.SerializeObject(responseChatDaySession)} \n requestInserDataChatBot: {JsonConvert.SerializeObject(requestInserDataChatBot)}", configChat.tenant_id, "Error");
                if (checkCount < int.Parse(configChat.Retry))
                {
                    ChangeDBChatDayChatMessageDaily(configChat, responseChatDaySession, requestInserDataChatBot, isAgentChat, checkCount++);
                }
            }
        }

        public void RequestUserSupport(RequestInsertDataChatBot requestInserDataChatBot, ResponseChatDaySession chatDaySession)
        {
            try
            {

                var chatBot = JsonConvert.DeserializeObject<Models.CreateFile.ChatBotMessage.RootChatBot>(requestInserDataChatBot.bodyData);
                if (chatBot != null && chatBot.@event.Contains("user_request_support"))
                {
                    var lst_urlLogService = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));

                    var requestSupport = new
                    {
                        chatDaySession = JsonConvert.SerializeObject(chatDaySession),
                        bodyDataSupport = requestInserDataChatBot.bodyData,
                        tenantId = requestInserDataChatBot.tenantId
                    };
                    foreach (var urlLog in lst_urlLogService)
                    {
                        try
                        {
                            _restAPI.SendChatBot(new RestAPIRequest()
                            {
                                Body = JsonConvert.SerializeObject(requestSupport),
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
                                Url = $"{urlLog}/api/SaveDataChatBot/CheckSessionRequestSupport"
                            });
                        }
                        catch (Exception ex)
                        {
                            new IboxLog($"Call API Request Support {ex.Message}", requestInserDataChatBot.tenantId, "Error");
                        }
                    }

                    CheckSessionRequestSupport(chatBot, requestInserDataChatBot.tenantId, chatDaySession);
                }

            }
            catch (Exception ex)
            {
                new IboxLog($"RequestUserSupport: {ex.Message}", requestInserDataChatBot.tenantId, "Error");
            }
        }

        public void CheckSessionRequestSupport(Models.CreateFile.ChatBotMessage.RootChatBot chatBot, string tenantId, ResponseChatDaySession chatDaySession)
        {
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChat == null)
                {
                    new IboxLog($"Config is null tenantId: {tenantId}", tenantId, "Error");
                    return;
                }

                int day = 5;
                while (day >= 0)
                {
                    try
                    {
                        string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                        string filePathNow = _commonData.GetFilePath(pathNow, DateTime.Now.AddDays(-day));
                        string PathdbDayNow = Path.Combine(filePathNow, $"chatbot.db");

                        if (!File.Exists(PathdbDayNow))
                        {
                            day--;
                            continue;
                        }

                        using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayNow))
                        {
                            try
                            {
                                ChatSessionDaily chatSessionDaily = dbContextDaily.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SessionId == chatDaySession.SessionId);

                                if (chatSessionDaily == null)
                                {
                                    new IboxLog($"ChatSessionId is null sessionId: {JsonConvert.SerializeObject(chatDaySession)}", tenantId, "Error");
                                    day--;
                                    continue;
                                }
                                else
                                {
                                    chatSessionDaily.IsSupport = true;
                                    dbContextDaily.Context.SaveChangesAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                new IboxLog($"Update chatSessionDaily: {ex.Message}", tenantId, "Error");
                            }

                            List<ChatMessageDaily> chatMessageDaily = dbContextDaily.Context.ChatMessageDailies.AsNoTracking().Where(ptr => ptr.SessionId == chatDaySession.SessionId).ToList();
                            if (chatMessageDaily == null || chatMessageDaily.Count == 0)
                            {
                                day--;
                                continue;
                            }

                            for (int i = 0; i < chatMessageDaily.Count; i++)
                            {
                                try
                                {
                                    if (chatMessageDaily[i].IsInputIC == true || string.IsNullOrEmpty(chatMessageDaily[i].MessageContent))
                                    {
                                        continue;
                                    }

                                    var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                                    var tenantContext = tContext.GetTenantContext(tenantId).Context;
                                    this._executeWF.SetTenantContext(tenantContext);
                                    var result = _executeWF.Execute(configChat.WF_SendChatIC, chatMessageDaily[i].MessageContent, tenantId);
                                    tenantContext.Context.Dispose();
                                    if (i == 0)
                                    {
                                        Thread.Sleep(500);
                                    }
                                    chatMessageDaily[i].IsInputIC = true;
                                    dbContextDaily.Context.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    new IboxLog($"WF Send IC: {ex.Message}", tenantId, "Error");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        new IboxLog($"while {ex.Message}", tenantId, "Error");
                    }
                    day--;
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"CheckSessionRequestSupport: {ex.Message}", tenantId, "Error");
            }
        }

        public void EndChatIC(CB_ConfigChat configChat, string tenantId, string senderId)
        {
            try
            {
                int day = 0;
                while (day < 2)
                {
                    try
                    {
                        string basePath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                        string filePath = _commonData.GetFilePath(basePath, DateTime.Now.AddDays(-day));
                        string PathdbDayDb = Path.Combine(filePath, $"chatbot.db");

                        if (!File.Exists(PathdbDayDb))
                        {
                            day++;
                            continue;
                        }

                        new IboxLog($"EndChatIC: senderId {senderId}", tenantId, "Info");
                        CloseChatDB(configChat, PathdbDayDb, tenantId, senderId, 0);
                    }
                    catch (Exception ex)
                    {
                        new IboxLog($"EndChatIC: {ex.Message}", tenantId, "Error");
                    }
                    day++;
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"EndChatIC: {ex.Message}", tenantId, "Error");
            }
        }

        public void CloseChatDB(CB_ConfigChat configChat, string PathdbDayDb, string tenantId, string senderId, int checkCount = 0)
        {
            try
            {
                ChatSessionDaily chatSessionDaily = new ChatSessionDaily();
                using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayDb))
                {

                    chatSessionDaily = dbContextDaily.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SenderId == senderId && ptr.IsClose == false);

                    if (chatSessionDaily != null)
                    {
                        chatSessionDaily.IsClose = true;
                        dbContextDaily.Context.SaveChangesAsync();
                    }
                }

                new IboxLog($"CloseChatDB chatSessionDaily: {JsonConvert.SerializeObject(chatSessionDaily)}", tenantId, "Info");
                try
                {
                    LstData DataInteraction = new LstData();
                    DataInteraction.Lst_Data = new List<BatchUpdateInteractionModel>();
                    if (chatSessionDaily != null && !string.IsNullOrEmpty(chatSessionDaily.InteractionCRM))
                    {
                        var obj = new BatchUpdateInteractionModel()
                        {
                            fld_lich_su_doan_chat_01 = (configChat.UrlShowChatIBox) + $"?tenantId={tenantId}&sessionId={chatSessionDaily.SessionId}&senderId={chatSessionDaily.SenderId}&date={DateTime.Now.ToString("yyyy-MM-dd")}",
                            _id = chatSessionDaily.InteractionCRM
                        };
                        DataInteraction.Lst_Data.Add(obj);


                        dynamic result = null;

                        var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                        var tenantContext1 = tContext1.GetTenantContext(tenantId).Context;
                        this._executeWF.SetTenantContext(tenantContext1);
                        result = _executeWF.Execute(configChat.WF_UpdateInteractionCRM, JsonConvert.SerializeObject(DataInteraction), tenantId, true);
                        tenantContext1.Context.Dispose();
                        new IboxLog($"response Update EndChat IC Batch Interaction: {result} \n body: {JsonConvert.SerializeObject(DataInteraction)}", tenantId, "Info");
                    }
                }
                catch (Exception ex)
                {
                    new IboxLog($"Update Update CRM CloseChatDB IC: {ex.Message}", tenantId, "Error");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CloseChatDB: {ex.Message}");
                if (checkCount < Int32.Parse(configChat.Retry))
                {
                    CloseChatDB(configChat, PathdbDayDb, tenantId, senderId, checkCount++);
                }
            }
        }

        public void EndChatAll()
        {
            try
            {
                foreach (var tenant in IBGlobalConfig.Tenants)
                {
                    var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenant.Id);
                    if (configChat == null)
                    {
                        continue;
                    }

                    DateTime threshold = DateTime.Now.AddMinutes(-Int32.Parse(configChat.EndChatTime));
                    LstData DataInteraction = new LstData();
                    DataInteraction.Lst_Data = new List<BatchUpdateInteractionModel>();
                    int day = 2;

                    while (day >= 0)
                    {
                        try
                        {
                            string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenant.Id);
                            string filePathDay = _commonData.GetFilePath(pathcurrent, DateTime.Now.AddDays(-day));
                            if (!Directory.Exists(filePathDay))
                            {
                                day--;
                                continue;
                            }

                            var dbFiles = Directory.GetFiles(filePathDay, "*.db", SearchOption.TopDirectoryOnly).ToList();
                            List<ChatSessionDaily> sessionDailies = GetActiveSenderFromMultipleDBs(dbFiles);
                            foreach (var session in sessionDailies)
                            {
                                var dbName = ConsistentHashing.GetChatBotDB(session.SenderId);
                                bool dayLast = CheckSessionEndChat(threshold, day, tenant.Id, session, dbName);
                                if (dayLast)
                                {
                                    var obj = new BatchUpdateInteractionModel()
                                    {
                                        sessionId = session.SessionId,
                                        senderId = session.SenderId,
                                        customerName = session.CustomerName,
                                        tag = session.Tag,
                                        fld_closed_by_01 = "bot",
                                        fld_interaction_channel_00000001 = session.Channel,
                                        fld_interaction_sub_channel_00000001 = session.SubChannel,
                                        fld_interaction_source_contact_id_00000001 = session.ContactId ?? null,
                                        fld_lich_su_doan_chat_01 = (configChat.UrlShowChatIBox) + $"?tenantId={tenant.Id}&sessionId={session.SessionId}&senderId={session.SenderId}&date={DateTime.Now.ToString("yyyy-MM-dd")}",
                                        _id = session.InteractionCRM
                                    };

                                    if (!DataInteraction.Lst_Data.Any(ptr => ptr.sessionId == session.SessionId))
                                    {
                                        DataInteraction.Lst_Data.Add(obj);
                                    }
                                }

                                try
                                {
                                    if (DataInteraction.Lst_Data.Count == 0)
                                    {
                                        Log.Information($"DataInteraction null ");
                                        day--;
                                        continue;
                                    }

                                    dynamic result = null;
                                    string wfName = "";
                                    var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                                    var tenantContext1 = tContext1.GetTenantContext(tenant.Id).Context;
                                    this._executeWF.SetTenantContext(tenantContext1);
                                    (result, wfName) = _executeWF.Execute(configChat.WF_UpdateInteractionCRM, JsonConvert.SerializeObject(DataInteraction), tenant.Id, true);
                                    tenantContext1.Context.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Call api update interaction: {ex.Message} \n lst_UpdateInteraction: {JsonConvert.SerializeObject(DataInteraction)}");
                                }
                            }

                            day--;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"EndChatIC: while {ex.Message}");
                            day--;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatIC: {ex.Message}");
            }
        }

        public bool CheckSessionEndChat(DateTime threshold, int day, string tenantId, ChatSessionDaily Session, string dbName)
        {
            try
            {
                bool checkUpdate = false;
                var activeDates = new List<DateTime>();
                string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                var currentDay = DateTime.Now.AddDays(-day);
                var today = DateTime.Now.Date;

                if (currentDay > today)
                {
                    return checkUpdate;
                }

                string filePathDay = _commonData.GetFilePath(pathcurrent, DateTime.Now.AddDays(-day));
                //Kiểm tra trong fodel có tồn tại file k
                if (!Directory.Exists(filePathDay))
                {
                    return false;
                }

                //Lấy tất cả session chưa close trong ngày
                var pathFileSender = Directory.GetFiles(filePathDay, "*.db", SearchOption.TopDirectoryOnly)
                .Where(file => Path.GetFileNameWithoutExtension(file).Contains(Path.GetFileNameWithoutExtension(dbName)))
                .OrderByDescending(file =>
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var parts = name.Split('_');

                    return int.TryParse(parts.Last(), out int shardNumber) ? shardNumber : int.MinValue;
                })
                .ToList();

                foreach (var pathDB in pathFileSender)
                {
                    using DBChatDayContext dbContext = _contextFactory.CreateContext(pathDB);
                    if (checkUpdate == true)
                    {
                        UpdateCloseSession(dbContext, Session.SessionId);
                        continue;
                    }

                    var messageSessions = dbContext.Context.ChatMessageDailies
                        .AsNoTracking()
                        .Where(x => x.SessionId == Session.SessionId)
                        .OrderByDescending(x => x.CreatedDate)
                        .FirstOrDefault();

                    if (messageSessions != null)
                    {
                        if (messageSessions.CreatedDate <= threshold)
                        {
                            if (!CheckSessionEndChat(threshold, day - 1, tenantId, Session, dbName))
                            {
                                checkUpdate = true;
                                UpdateCloseSession(dbContext, Session.SessionId);
                            }
                        }
                    }
                }

                return checkUpdate;
            }
            catch (Exception ex)
            {
                Log.Error($"CheckSessionEndChat: {ex.Message} \n day: {day}, tenantId: {tenantId}, SessionId :{Session}  ");
                return false;
            }
        }
        public void UpdateCloseSession(DBChatDayContext dbContext, string sessionId)
        {
            try
            {
                var sessionChat = dbContext.Context.ChatSessionDailies.FirstOrDefault(x => x.SessionId == sessionId);
                if (sessionChat != null)
                {
                    sessionChat.IsClose = true;
                    dbContext.Context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateCloseSession: {ex.Message}");
            }
        }

        public Models.CreateFile.IBox.ListDataIBox ShowDataChatBot(string? senderId, string? tenantId, string? sessionId, string? dateTimeCurrent)
        {
            try
            {
                Models.CreateFile.IBox.ListDataIBox listDataIBox = new Models.CreateFile.IBox.ListDataIBox();
                listDataIBox.message = new List<Models.CreateFile.IBox.MessageIBox>();

                var logServiceIBox = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                foreach (var urlLog in logServiceIBox)
                {
                    try
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
                            Method = "Get",
                            Timeout = 30,
                            Url = $"{urlLog}/api/SaveDataChatBot/GetDataChatBot/{senderId}/{tenantId}/{sessionId}/{dateTimeCurrent}"
                        });

                        if (resultSenderId == null || string.IsNullOrEmpty(resultSenderId.Result))
                        {
                            continue;
                        }

                        var lst_DataChatBot = JsonConvert.DeserializeObject<Models.CreateFile.IBox.ListDataIBox>(resultSenderId.Result);

                        if (lst_DataChatBot == null || lst_DataChatBot.message == null || lst_DataChatBot.message.Count == 0)
                        {
                            continue;
                        }

                        listDataIBox.bot_code = string.IsNullOrEmpty(listDataIBox.bot_code) ? lst_DataChatBot.bot_code : "";
                        listDataIBox.sender_id = string.IsNullOrEmpty(listDataIBox.sender_id) ? lst_DataChatBot.sender_id : "";
                        listDataIBox.sender_name = string.IsNullOrEmpty(lst_DataChatBot.sender_name) && string.IsNullOrEmpty(listDataIBox.sender_name) ? "KH" : lst_DataChatBot.sender_name;
                        listDataIBox.channel = string.IsNullOrEmpty(listDataIBox.channel) ? lst_DataChatBot.channel : listDataIBox.channel;
                        listDataIBox.sub_channel = string.IsNullOrEmpty(listDataIBox.sub_channel) ? lst_DataChatBot.sub_channel : listDataIBox.sub_channel;
                        listDataIBox.message.AddRange(lst_DataChatBot.message);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"For Call API GetDataChatBot: {ex.Message}");
                    }
                }

                var lst_ChatBot = GetDataChatBot(senderId, tenantId, sessionId, dateTimeCurrent);
                if (lst_ChatBot != null && lst_ChatBot.message != null && lst_ChatBot.message.Count > 0)
                {
                    listDataIBox.message.AddRange(lst_ChatBot.message);
                }

                listDataIBox.bot_code = lst_ChatBot != null && !string.IsNullOrEmpty(lst_ChatBot.bot_code) ? lst_ChatBot.bot_code : listDataIBox.bot_code;
                listDataIBox.sender_id = lst_ChatBot != null && !string.IsNullOrEmpty(lst_ChatBot.sender_id) ? lst_ChatBot.sender_id : listDataIBox.sender_id;
                listDataIBox.sender_name = lst_ChatBot != null && !string.IsNullOrEmpty(lst_ChatBot.sender_name) ? lst_ChatBot.sender_name : listDataIBox.sender_name;
                listDataIBox.channel = lst_ChatBot != null && !string.IsNullOrEmpty(lst_ChatBot.channel) ? lst_ChatBot.channel : listDataIBox.channel;
                listDataIBox.sub_channel = lst_ChatBot != null && !string.IsNullOrEmpty(lst_ChatBot.sub_channel) ? lst_ChatBot.sub_channel : listDataIBox.sub_channel;
                listDataIBox.message = listDataIBox.message.OrderByDescending(ptr => _commonData.ToDate1(ptr.timestamp)).ToList();
                return listDataIBox;
            }
            catch (Exception ex)
            {
                Log.Error($"ShowDataChatBot: {ex.Message}");
                return new Models.CreateFile.IBox.ListDataIBox();
            }
        }

        public CB_Customer GetDataCustomer(string tenantId, string? senderId, string? idBot)
        {
            try
            {
                var urlLog = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                CB_Customer customerNew = new CB_Customer();
                CB_Customer customer1 = new CB_Customer();
                foreach (var url in urlLog)
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
                             }
                        },
                        Method = "GET",
                        Timeout = 30,
                        Url = $"{url}/api/ShowData/GetCustomer/{tenantId}/{senderId}/{idBot}"
                    });

                    if (result != null && !string.IsNullOrEmpty(result.Result))
                    {

                        customer1 = JsonConvert.DeserializeObject<CB_Customer>(result.Result);


                        if (customer1 != null)
                        {
                            customerNew.ContactId = string.IsNullOrEmpty(customerNew.ContactId) ? customer1.ContactId : customerNew.ContactId;
                            customerNew.SenderId = string.IsNullOrEmpty(customerNew.SenderId) ? customer1.SenderId : customerNew.SenderId;
                            customerNew.CustomerInfo = string.IsNullOrEmpty(customerNew.CustomerInfo) ? customer1.CustomerInfo : customerNew.CustomerInfo;
                            customerNew.cif_list_data = string.IsNullOrEmpty(customerNew.cif_list_data) ? customer1.cif_list_data : customerNew.cif_list_data;
                            customerNew.Phone = string.IsNullOrEmpty(customerNew.Phone) ? customer1.Phone : customerNew.Phone;
                        }
                    }
                }

                ChatSessionDaily chatSessionDaily = new ChatSessionDaily();

                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(tenantId).Context;
                var customer = tenantContext.Context.CB_Customers.AsNoTracking().FirstOrDefault(ptr => ptr.SenderId == senderId);

                if (customer != null)
                {
                    customerNew.ContactId = string.IsNullOrEmpty(customerNew.ContactId) ? customer.ContactId : customerNew.ContactId;
                    customerNew.SenderId = string.IsNullOrEmpty(customerNew.SenderId) ? customer.SenderId : customerNew.SenderId;
                    customerNew.CustomerInfo = string.IsNullOrEmpty(customerNew.CustomerInfo) ? customer.CustomerInfo : customerNew.CustomerInfo;
                    customerNew.cif_list_data = string.IsNullOrEmpty(customerNew.cif_list_data) ? customer.cif_list_data : customerNew.cif_list_data;
                    customerNew.Phone = string.IsNullOrEmpty(customerNew.Phone) ? customer.Phone : customerNew.Phone;
                }

                tenantContext.Context.Dispose();
                return customerNew;
            }
            catch (Exception ex)
            {
                Log.Error($"GetDataCustomer: {ex.Message} \n senderid: {senderId}, tenantId: {tenantId}, IdBot: {idBot}");
                return new CB_Customer();
            }
        }

        public CB_Customer GetCustomer(string tenantId, string senderId, string? idBot)
        {
            try
            {
                ChatSessionDaily chatSessionDaily = new ChatSessionDaily();

                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(tenantId).Context;

                CB_Customer customer = tenantContext.Context.CB_Customers.AsNoTracking().FirstOrDefault(ptr => ptr.SenderId == senderId);


                if (customer == null)
                {
                    return new CB_Customer();
                }

                CB_Customer customer1 = new CB_Customer()
                {
                    cif_list_data = customer.cif_list_data,
                    ContactId = customer.ContactId,
                    CreatedDate = customer.CreatedDate,
                    CustomerInfo = customer.CustomerInfo,
                    Id = customer.Id,
                    IsDelete = customer.IsDelete,
                    ModificationDate = customer.ModificationDate,
                    Phone = customer.Phone,
                    SenderId = customer.SenderId,
                };

                return customer1;
            }
            catch (Exception ex)
            {
                Log.Error($"GetCustomer: {ex.Message} \n tenantId: {tenantId}, senderId:{senderId}, idBot:{idBot}");
            }

            return new CB_Customer();
        }

        public void ChatBotSendIC(RequestShowData requestShowData)
        {
            try
            {
                SendDataChatBotToIC(requestShowData);
            }
            catch (Exception ex)
            {
                Log.Error($"ChatBotSendIC: {ex.Message}");
            }
        }

        public void SendDataChatBotToIC(RequestShowData requestShowData)
        {
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == requestShowData.tenantId);
                if (configChat == null)
                {
                    Log.Error($"SendDataChatBotToIC ConfigChat is not null tenantId: {requestShowData.tenantId}");
                    return;
                }

                DateTimeOffset dto = new DateTimeOffset(DateTime.Now);
                long timestamp = dto.ToUnixTimeMilliseconds();

                var rootChatBot = new ChatBotMessage.RootChatBot()
                {
                    timestamp = timestamp,
                    bot_code = configChat.IdBOT,
                    @event = "user_sent_message",
                    data = new ChatBotMessage.Data()
                    {
                        sender = new ChatBotMessage.Sender()
                        {
                            id = requestShowData.senderId,
                            name = requestShowData.customerName
                        },
                        channel = requestShowData.channel,
                        sub_channel = requestShowData.subchannel,
                        message = new Models.CreateFile.ChatBotMessage.Message()
                        {
                            type = "text",
                            content = new Models.CreateFile.ChatBotMessage.Content()
                            {
                                text = "Agent hỗ trợ KH"
                            }
                        },
                        session_id = requestShowData.sessionId
                    }
                };

                try
                {
                    var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                    var tenantContext = tContext.GetTenantContext(requestShowData.tenantId).Context;
                    this._executeWF.SetTenantContext(tenantContext);
                    var result = _executeWF.Execute(configChat.WF_SendChatIC, JsonConvert.SerializeObject(rootChatBot), requestShowData.tenantId);
                    tenantContext.Context.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error($"Đẩy data Agent hỗ trợ KH vào IC: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendDataChatBotToIC: {ex.Message}");
            }
        }

        public List<ResponseChatBotGetSenderId> ShowAllDataChatBot(RequestShowData requestShowData)
        {
            try
            {
                List<ResponseChatBotGetSenderId> chatBotGetSenderId = new List<ResponseChatBotGetSenderId>();
                var logServiceIBox = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                foreach (var urlLog in logServiceIBox)
                {
                    try
                    {
                        var resultGetDataChatSender = _restAPI.SendChatBot(new RestAPIRequest()
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
                            Method = "Get",
                            Timeout = 30,
                            Url = $"{urlLog}/api/SaveDataChatBot/GetDataChatBotSenderId/{requestShowData.tenantId}/{requestShowData.senderId}"
                        });

                        if (resultGetDataChatSender == null || string.IsNullOrEmpty(resultGetDataChatSender.Result))
                        {
                            continue;
                        }

                        var lst_DataChatBot = JsonConvert.DeserializeObject<List<ResponseChatBotGetSenderId>>(resultGetDataChatSender.Result);

                        if (lst_DataChatBot == null)
                        {
                            continue;
                        }

                        chatBotGetSenderId.AddRange(lst_DataChatBot);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"For Call API GetDataChatBot: {ex.Message}");
                    }
                }

                chatBotGetSenderId.AddRange(GetDataChatBotSenderId(requestShowData.tenantId, requestShowData.senderId));
                return chatBotGetSenderId;
            }
            catch (Exception ex)
            {
                Log.Error($"ShowAllDataChatBot: {ex.Message}");
                return new List<ResponseChatBotGetSenderId>();
            }
        }

        public List<ResponseChatBotGetSenderId> GetDataChatBotSenderId(string? tenantId, string? senderId)
        {
            try
            {
                if (tenantId == null || senderId == null)
                {
                    return new List<ResponseChatBotGetSenderId>();
                }

                string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, DateTime.Now);
                string PathdbDayDb = Path.Combine(filePathcurrent, $"chatbot.db");
                List<ResponseChatBotGetSenderId> lst_responseChatBotGetSender = new List<ResponseChatBotGetSenderId>();
                using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayDb))
                {
                    var chatSessionDailys = dbContextDaily.Context.ChatSessionDailies.AsNoTracking().Where(ptr => ptr.SenderId == senderId);

                    foreach (var chatSessionDaily in chatSessionDailys)
                    {
                        var chatMessageDailys = dbContextDaily.Context.ChatMessageDailies.AsNoTracking().Where(ptr => ptr.SessionId == chatSessionDaily.SessionId).ToList();
                        ResponseChatBotGetSenderId responseChatBotGetSenderId = new ResponseChatBotGetSenderId()
                        {
                            Channel = chatSessionDaily.Channel,
                            DateMessageLast = chatSessionDaily.DateMessageLast,
                            InteractionCRM = chatSessionDaily.InteractionCRM,
                            IsClose = chatSessionDaily.IsClose,
                            IsSupport = chatSessionDaily.IsSupport,
                            SenderId = chatSessionDaily.SenderId,
                            SessionId = chatSessionDaily.SessionId,
                            SubChannel = chatSessionDaily.SubChannel,
                            chatMessageDailys = chatMessageDailys
                        };
                        lst_responseChatBotGetSender.Add(responseChatBotGetSenderId);
                    }
                }

                return lst_responseChatBotGetSender;
            }
            catch (Exception ex)
            {
                Log.Error($"GetDataChatBotSenderId: {ex.Message}");
                return new List<ResponseChatBotGetSenderId>();
            }
        }

        public Models.CreateFile.IBox.ListDataIBox GetDataChatBot(string senderId, string tenantId, string sessionId, string dateTimeCurrent)
        {
            try
            {
                DateTime dateTime = _commonData.ToDate1(dateTimeCurrent);
                string pathcurrent = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, dateTime);
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChat == null)
                {
                    return new ListDataIBox();
                }

                string dbName = ConsistentHashing.GetChatBotDB(senderId);

                List<string> pathNames = ConvertPathDB(Path.Combine(filePathcurrent, dbName), dateTime, configChat);


                List<ChatMessageDaily> lst_messageDaily = new List<ChatMessageDaily>();
                Models.CreateFile.IBox.ListDataIBox listDataIBox = new Models.CreateFile.IBox.ListDataIBox();
                listDataIBox.message = new List<Models.CreateFile.IBox.MessageIBox>();

                foreach (var pathFile in pathNames)
                {
                    using (var dbContextDaily = _contextFactory.CreateContext(pathFile))
                    {
                        lst_messageDaily = dbContextDaily.Context.ChatMessageDailies.AsNoTracking().Where(ptr => ptr.SessionId == sessionId).ToList();
                        if (lst_messageDaily != null)
                        {
                            foreach (var messageDaily in lst_messageDaily)
                            {
                                try
                                {
                                    var chatBot = JsonConvert.DeserializeObject<ChatBot.DB.Models.CreateFile.ChatBotMessage.RootChatBot>(messageDaily.MessageContent);
                                    if (chatBot == null)
                                    {
                                        continue;
                                    }

                                    if (!chatBot.@event.ToLower().Contains("bot_sent_message") && !chatBot.@event.ToLower().Contains("user_sent_message") && !chatBot.@event.ToLower().Contains("agent_sent_message") && !chatBot.@event.ToLower().Contains("agent_chat"))
                                    {
                                        continue;
                                    }

                                    if (chatBot.data.message.content.file_name != null)
                                    {
                                        chatBot.data.message.type = chatBot.data.message.content.file_name.Contains("image") == true ? "file" : chatBot.data.message.type;
                                    }


                                    if (chatBot.data.message.content.buttons != null)
                                    {
                                        chatBot.data.message.type = "buttons";
                                    }


                                    if (chatBot.data.message.type.ToUpper().Contains("PAYLOAD"))
                                    {
                                        chatBot.data.message.type = "text";
                                    }
                                    else if (chatBot.data.message.type.ToLower().Contains("image") || chatBot.data.message.type.ToLower().Contains("video") || chatBot.data.message.type.ToLower().Contains("audio") || chatBot.data.message.type.ToLower().Contains("link"))
                                    {
                                        chatBot.data.message.type = "file";
                                    }


                                    if (chatBot.data.message.type.ToUpper().Contains("QUICK_REPLY") && chatBot.data.message.content.buttons != null)
                                    {
                                        chatBot.data.message.type = "buttons";
                                    }
                                    else if (chatBot.data.message.type.ToUpper().Contains("QUICK_REPLY") && chatBot.data.message.content.buttons == null)
                                    {
                                        chatBot.data.message.type = "text";
                                    }
                                    else if (chatBot.data.message.type.ToUpper().Contains("CAROUSEL"))
                                    {
                                        chatBot.data.message.type = "carousel";
                                    }

                                    if (!chatBot.data.message.type.ToLower().Contains("text") && !chatBot.data.message.type.ToLower().Contains("file") && !chatBot.data.message.type.ToLower().Contains("carousel") && !chatBot.data.message.type.ToLower().Contains("buttons"))
                                    {
                                        continue;
                                    }
                                    int bot = 0;
                                    switch (chatBot.@event.ToLower())
                                    {
                                        case "bot_sent_message":
                                            bot = 1;
                                            break;

                                        case "user_sent_message":
                                            bot = 0;
                                            break;

                                        case "agent_chat":
                                            bot = 2;
                                            break;

                                        case "agent_sent_message":
                                            bot = 3;
                                            continue;
                                        default:
                                            bot = 3;
                                            continue;
                                    }

                                    listDataIBox.bot_code = !string.IsNullOrEmpty(chatBot.bot_code) ? chatBot.bot_code : listDataIBox.bot_code;
                                    listDataIBox.sender_id = !string.IsNullOrEmpty(chatBot.data.sender.id) ? chatBot.data.sender.id : listDataIBox.sender_id;
                                    listDataIBox.sender_name = !string.IsNullOrEmpty(chatBot.data.sender.name) ? chatBot.data.sender.name : "KH";
                                    listDataIBox.channel = chatBot.data.channel;
                                    listDataIBox.sub_channel = !string.IsNullOrEmpty(chatBot.data.sub_channel) ? chatBot.data.sub_channel : listDataIBox.sub_channel;
                                    listDataIBox.message.Add(new Models.CreateFile.IBox.MessageIBox
                                    {
                                        id = chatBot.id,
                                        BOT = bot,
                                        type = chatBot.data.message.type,
                                        text = !string.IsNullOrEmpty(chatBot.data.message.content.text) ? chatBot.data.message.content.text.Split('#')[chatBot.data.message.content.text.Split('#').Count() - 1].Replace("\n", "").Replace("\"", "'").Replace("\t", " ") : "",
                                        carousel_cards = lstCarouse(chatBot),
                                        fileIc = fileIC(chatBot),
                                        buttons = lstButtons(chatBot),
                                        timestamp = string.IsNullOrEmpty(chatBot.timestamp.ToString()) || chatBot.timestamp.ToString() == "0" ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : chatBot.timestamp <= 253402300799 && chatBot.timestamp >= 0 ? DateTimeOffset.FromUnixTimeSeconds(chatBot.timestamp).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") : DateTimeOffset.FromUnixTimeSeconds(chatBot.timestamp / 1000).UtcDateTime.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"),
                                        ThisSite = IBGlobalConfig.ThisSite
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"for get messageDaily convert show data: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                listDataIBox.message = listDataIBox.message.OrderByDescending(x => _commonData.ToDate1(x.timestamp)).ToList();
                return listDataIBox;

            }
            catch (Exception ex)
            {
                Log.Error($"GetDataChatBot: {ex.Message}");
                return new Models.CreateFile.IBox.ListDataIBox();
            }
        }

        public List<Models.CreateFile.IBox.Carousel_cardsIBox>? lstCarouse(DB.Models.CreateFile.ChatBotMessage.RootChatBot rootChatBot)
        {
            try
            {

                if (!rootChatBot.data.message.type.ToUpper().Contains("CAROUSEL"))
                {
                    return null;
                }

                List<Models.CreateFile.IBox.Carousel_cardsIBox> lstcarousel_CardsIBoxes = new List<Models.CreateFile.IBox.Carousel_cardsIBox>();
                if (rootChatBot?.data?.message?.content?.carousel_cards != null)
                {
                    foreach (var item in rootChatBot.data.message.content.carousel_cards)
                    {
                        List<Models.CreateFile.IBox.ButtonsIBox> lstButton = new List<Models.CreateFile.IBox.ButtonsIBox>();

                        foreach (var button in item.buttons)
                        {
                            lstButton.Add(new Models.CreateFile.IBox.ButtonsIBox()
                            {
                                payload = button.payload,
                                title = button.title,
                            });
                        }

                        lstcarousel_CardsIBoxes.Add(new Models.CreateFile.IBox.Carousel_cardsIBox()
                        {
                            title = item.title,
                            subtitle = item.subtitle,
                            item_url = item.item_url,
                            image_url = item.image_url,
                            buttons = lstButton
                        });
                    }
                }

                return lstcarousel_CardsIBoxes;
            }
            catch (Exception ex)
            {
                Log.Error($"lstCarouse: {ex.Message}");
                return null;
            }
        }

        public FileIC? fileIC(DB.Models.CreateFile.ChatBotMessage.RootChatBot rootChatBot)
        {
            try
            {

                if (!rootChatBot.data.message.type.ToUpper().Contains("FILE"))
                {
                    return null;
                }

                return new FileIC()
                {
                    fileName = rootChatBot?.data?.message?.content?.file_name ?? "",
                    url = rootChatBot?.data?.message?.content?.url ?? ""
                };
            }
            catch (Exception ex)
            {
                Log.Error($"fileIC: {ex.Message}");
                return new FileIC();
            }
        }

        public List<Models.CreateFile.IBox.ButtonsIBox>? lstButtons(DB.Models.CreateFile.ChatBotMessage.RootChatBot rootChatBot)
        {
            try
            {
                if (!rootChatBot.data.message.type.ToUpper().Contains("BUTTONS"))
                {
                    return null;
                }

                List<Models.CreateFile.IBox.ButtonsIBox> lstButton = new List<Models.CreateFile.IBox.ButtonsIBox>();

                foreach (var item in rootChatBot.data.message.content.buttons)
                {
                    lstButton.Add(new Models.CreateFile.IBox.ButtonsIBox()
                    {
                        title = item.title,
                        payload = item.payload,
                    });
                }

                return lstButton;
            }
            catch (Exception ex)
            {
                Log.Error($"lstButtons: {ex.Message}");
                return null;
            }
        }

        public void ZipFileChatBot()
        {
            try
            {
                foreach (var configChat in IBGlobalTenantConfig.ConfigChatBots)
                {
                    try
                    {
                        string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, configChat.tenant_id);
                        List<string> directories = new List<string>();
                        string path = "";
                        for (int i = 2; i < 6; i++)
                        {
                            string pathdbHistory = _createDB.GetFilePath(pathNow, DateTime.Now.AddDays(-i));
                            if (!Directory.Exists(pathdbHistory))
                            {
                                continue;
                            }

                            directories.Add(pathdbHistory);
                            path += "_" + DateTime.Now.AddDays(-i).ToString("yyyyMMdd");
                        }

                        string tempDirectory = Path.Combine(Path.GetTempPath(), $"TempZip_{Guid.NewGuid()}");
                        _commonData.CreateFolder1(tempDirectory);

                        string pathDBZip = Path.Combine(Directory.GetCurrentDirectory(), configChat.PathBackUp, configChat.tenant_id);
                        string zipFileName = $"BackupChatBot{path}.zip";
                        string zipFilePath = Path.Combine(pathDBZip, zipFileName);

                        try
                        {

                            _commonData.CreateFolder(zipFilePath);

                            if (File.Exists(zipFilePath))
                            {
                                File.Delete(zipFilePath);
                            }

                            foreach (var directory in directories)
                            {
                                try
                                {
                                    string relativePath = Path.GetRelativePath(pathNow, directory);

                                    string fullPath = Path.Combine(tempDirectory, relativePath);
                                    CopyDirectory(directory, fullPath);
                                    Directory.Delete(directory, true);
                                }
                                catch (Exception)
                                {
                                    Log.Error($"Copy and Delete: {directory}");
                                }
                            }

                            if (directories != null && directories.Count > 0)
                            {
                                ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);
                            }
                        }
                        finally
                        {
                            Directory.Delete(tempDirectory, true);
                        }

                        var zipFiles = Directory.GetFiles(pathDBZip, "BackupChatBot*.zip")
                                                .Select(f => new FileInfo(f))
                                                .OrderByDescending(f => f.CreationTime)
                                                .ToList();

                        var oldZipFiles = zipFiles.Skip(Int32.Parse(configChat.FileDay));

                        foreach (var file in oldZipFiles)
                        {
                            try
                            {
                                file.Delete();
                                Log.Information($"Deleted old zip file: {file.FullName}");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to delete zip file {file.FullName}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Zip file for config {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Zip file {ex.Message}");
            }
        }

        public void RemoveFileZip()
        {
            try
            {
                foreach (var configChat in IBGlobalTenantConfig.ConfigChatBots)
                {
                    string extractPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryFolderZip, configChat.tenant_id);
                    if (!Directory.Exists(extractPath))
                    {
                        continue;
                    }

                    Directory.Delete(extractPath, true);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RemoveFileZip: {ex.Message}");
            }
        }

        public void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                _commonData.CreateFolder1(destDir);
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile);
                }

                foreach (var subDir in Directory.GetDirectories(sourceDir))
                {
                    string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    CopyDirectory(subDir, destSubDir);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CopyDirectory: {ex.Message}");
            }
        }

        public void SaveDataChat(CB_ConfigChat configChat)
        {
            string content = "";
            string newFilePath = "";
            string filePathOld = "";
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryFolder, configChat.tenant_id);

                if (!Directory.Exists(path) || !Directory.GetFiles(path).Any())
                {
                    return;
                }

                var mostRecentFile = Directory.GetFiles(path)
                    .Select(file => new FileInfo(file))
                    .OrderBy(fileInfo => fileInfo.LastWriteTime)
                    .FirstOrDefault();

                int valueSecond = _commonData.ShowSecond();
                if (mostRecentFile == null || mostRecentFile.Name.Contains(DateTime.Now.ToString("yyyyMMddHHmm") + valueSecond))
                {
                    return;
                }

                string newFolderPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolder);
                filePathOld = mostRecentFile.FullName;

                _commonData.CreateFolder1(newFolderPath);
                newFilePath = Path.Combine(newFolderPath, mostRecentFile.Name);
                File.Move(mostRecentFile.FullName, newFilePath);

                try
                {
                    if (File.Exists(mostRecentFile.FullName) && File.Exists(newFilePath))
                    {
                        File.Delete(mostRecentFile.FullName);
                    }
                }
                catch (IOException ex)
                {
                    Log.Warning($"Delete Chat: {ex.Message}\n Path {newFilePath}");
                }

                using (var reader = new StreamReader(newFilePath))
                {
                    content = reader.ReadToEnd().TrimEnd(',');
                }

                content = $"{{\"Data\":[{content}]}}";
                DataFileTemp? dataFileTemp = JsonConvert.DeserializeObject<DataFileTemp>(content);
                if (dataFileTemp == null || dataFileTemp.Data == null)
                {
                    return;
                }

                try
                {
                    var groupedData = dataFileTemp.Data
                    .Where(ptr => ptr != null)
                    .GroupBy(ptr => ptr.senderId)
                    .ToHashSet();

                    var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                    var tenantContext = context.GetTenantContext(configChat.tenant_id).Context;
                    _executeWF.SetTenantContext(tenantContext);

                    try
                    {
                        ProcessSenderGroup(tenantContext, groupedData, configChat);
                        File.Delete(newFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error processing senderId: {JsonConvert.SerializeObject(groupedData)}");
                    }

                    tenantContext.Context.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error($"Transaction Error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SaveDataChat: {ex}");
                try
                {
                    if (File.Exists(filePathOld) && File.Exists(newFilePath))
                    {
                        File.Delete(newFilePath);
                    }
                }
                catch (IOException ex1)
                {
                    Log.Warning($"Delete Chat: {ex1.Message}\n Path {newFilePath}");
                }
            }
        }
        private void ProcessSenderGroup(TenantContext tenantContext, HashSet<IGrouping<string, DataFile>> groupedData, CB_ConfigChat configChat)
        {
            var customersNew = new List<CB_Customer>();
            var tags = new TagModel { Data = new List<TagSub>() };
            var rootChatBots = new List<ChatBotMessage.RootChatBot>();

            var chatMessagesByDB = new Dictionary<string, List<ChatMessageDaily>>();
            var chatSessionsByDB = new Dictionary<string, List<ChatSessionDaily>>();

            var senderIds = groupedData.Select(g => g.Key).Distinct().ToList();
            var dbPathsTodayBySenderId = new Dictionary<string, List<string>>();
            var dbPathsYesterdayBySenderId = new Dictionary<string, List<string>>();
            string basePath = Path.Combine(DataPath.DataBaseChatBot, configChat.tenant_id);
            string dirCurrent = Directory.GetCurrentDirectory();

            var existingCustomersDict = tenantContext.Context.CB_Customers
                .AsNoTracking()
                .Where(c => senderIds.Contains(c.SenderId))
                .ToDictionary(c => c.SenderId);

            foreach (var senderId in senderIds)
            {
                var dbName = ConsistentHashing.GetChatBotDB(senderId);

                var fullPathToday = Path.Combine(dirCurrent, _commonData.GetFilePath(basePath, DateTime.Now), dbName);
                var fullPathYesterday = Path.Combine(dirCurrent, _commonData.GetFilePath(basePath, DateTime.Now.AddDays(-1)), dbName);
                dbPathsTodayBySenderId[senderId] = ConvertPathDB(fullPathToday, DateTime.Now, configChat);
                dbPathsYesterdayBySenderId[senderId] = ConvertPathDB(fullPathYesterday, DateTime.Now.AddDays(-1), configChat);
            }

            var dbPathsTodayDict = PrepareDbPaths(senderIds, configChat.tenant_id, true);
            var dbPathsYesterdayDict = PrepareDbPaths(senderIds, configChat.tenant_id, false);

            var activeSessionsToday = GetActiveSessionsFromMultipleDBs(dbPathsTodayDict);
            var activeSessionsYesterday = GetActiveSessionsFromMultipleDBs(dbPathsYesterdayDict);

            foreach (var group in groupedData)
            {
                var senderId = group.Key;
                var chatBotDB = ConsistentHashing.GetChatBotDB(senderId);
                var pathCurrent = Path.Combine(DataPath.DataBaseChatBot, configChat.tenant_id);
                var dbPathsToday = ConvertPathDB(Path.Combine(Directory.GetCurrentDirectory(), _commonData.GetFilePath(pathCurrent, DateTime.Now), chatBotDB), DateTime.Now, configChat);
                var dbPathsYesterday = ConvertPathDB(Path.Combine(Directory.GetCurrentDirectory(), _commonData.GetFilePath(pathCurrent, DateTime.Now.AddDays(-1)), chatBotDB), DateTime.Now.AddDays(-1), configChat);

                var dbPath = dbPathsToday.FirstOrDefault();
                if (string.IsNullOrEmpty(dbPath)) continue;

                activeSessionsYesterday.TryGetValue(group.Key, out var sessionYesterday);
                activeSessionsToday.TryGetValue(group.Key, out var sessionToday);

                var prioritizedItem = group.OrderByDescending(ptr => ptr.isSupport).FirstOrDefault();
                var subChannelItem = group.FirstOrDefault(ptr => !string.IsNullOrEmpty(ptr.sub_channel));
                if (prioritizedItem == null) continue;

                if (!existingCustomersDict.ContainsKey(senderId))
                {
                    customersNew.Add(new CB_Customer { SenderId = senderId, CreatedDate = DateTime.Now });
                }

                var isSupport = SessionChatBot.GetSession(senderId)?.IsSupport ?? false;
                var sessionId = sessionYesterday?.SessionId ?? sessionToday?.SessionId ?? Guid.NewGuid().ToString();

                var chatSessionDaily = new ChatSessionDaily
                {
                    Channel = prioritizedItem.channel ?? "livechat",
                    IsClose = false,
                    IsSupport = isSupport,
                    SenderId = senderId,
                    SessionId = sessionId,
                    SubChannel = subChannelItem?.sub_channel ?? "",
                    DateMessageLast = DateTime.Now,
                    CustomerName = prioritizedItem.name ?? "KH",
                    ContactId = existingCustomersDict.ContainsKey(senderId)
                    ? existingCustomersDict[senderId].ContactId
                    : "",
                    InteractionCRM = sessionYesterday?.InteractionCRM ?? sessionToday?.InteractionCRM ?? "",
                    IsSiteClose = sessionYesterday?.IsSiteClose ?? sessionToday?.IsSiteClose ?? IBGlobalConfig.ThisSite
                };

                if (sessionToday == null)
                {
                    if (!chatSessionsByDB.ContainsKey(dbPath)) chatSessionsByDB[dbPath] = new List<ChatSessionDaily>();
                    chatSessionsByDB[dbPath].Add(chatSessionDaily);
                }

                if (prioritizedItem.isSupport)
                {
                    var rootChatBot = new ChatBotMessage.RootChatBot
                    {
                        timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        bot_code = configChat.IdBOT,
                        @event = "user_sent_message",
                        data = new ChatBotMessage.Data
                        {
                            sender = new ChatBotMessage.Sender { id = senderId, name = chatSessionDaily.CustomerName },
                            channel = chatSessionDaily.Channel,
                            sub_channel = chatSessionDaily.SubChannel,
                            message = new Models.CreateFile.ChatBotMessage.Message
                            {
                                type = "text",
                                content = new Models.CreateFile.ChatBotMessage.Content { text = "KH yêu cầu gặp tư vấn viên" }
                            },
                            session_id = sessionId
                        }
                    };

                    rootChatBots.Add(rootChatBot);

                    var message = new ChatMessageDaily
                    {
                        IsInputIC = true,
                        SessionId = sessionId,
                        MessageContent = JsonConvert.SerializeObject(rootChatBot)
                    };
                    if (!chatMessagesByDB.ContainsKey(dbPath)) chatMessagesByDB[dbPath] = new List<ChatMessageDaily>();
                    chatMessagesByDB[dbPath].Add(message);
                }

                foreach (var ptr in group)
                {
                    if (ptr == null || string.IsNullOrEmpty(ptr.bodyData)) continue;

                    var isSupportItem = SessionChatBot.GetSession(ptr.senderId)?.IsSupport == true;
                    var chatMessage = new ChatMessageDaily
                    {
                        IsInputIC = isSupportItem,
                        SessionId = sessionId,
                        MessageContent = ptr.bodyData
                    };

                    if (isSupportItem && ptr.isInputIC == false)
                    {
                        try
                        {
                            _executeWF.Execute(configChat.WF_SendChatIC, ptr.bodyData, configChat.tenant_id);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"SendChatIC: {ex.Message}, body: {ptr.bodyData}");
                        }
                    }

                    try
                    {
                        var chatBot = JsonConvert.DeserializeObject<ChatBotMessage.RootChatBot>(ptr.bodyData);
                        if (chatBot?.data?.message?.content?.tags != null && chatBot.data.message.type == "tag")
                        {
                            foreach (var item in chatBot.data.message.content.tags)
                            {
                                var sessionChat = SessionChatBot.GetSession(senderId);
                                tags.Data.Add(new TagSub
                                {
                                    tag = item?.tag_name ?? "",
                                    senderId = senderId,
                                    sessionId = sessionId,
                                    channel = chatBot.data.channel ?? "livechat",
                                    subChannel = subChannelItem?.sub_channel ?? "",
                                    customerName = prioritizedItem.name ?? "KH",
                                    contactId = sessionChat?.ContactId ?? ""
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"TagParse: {ex.Message} body: {ptr.bodyData}");
                    }

                    if (!chatMessagesByDB.ContainsKey(dbPath)) chatMessagesByDB[dbPath] = new List<ChatMessageDaily>();
                    chatMessagesByDB[dbPath].Add(chatMessage);
                }
            }

            foreach (var kvp in chatSessionsByDB)
            {
                customersNew = CreateInteractionCRM(customersNew, new List<string> { kvp.Key }, kvp.Value, configChat);
            }

            var (columns, values) = ExtractColumnsAndValues(customersNew);
            ExecuteMerge(
                configChat.tenant_id,
                "CB_Customers",
                "SenderId",
                columns,
                values
            );

            //InsertOrUpdateCustomersWithRetry(customersNew, tenantContext);
            foreach (var kvp in chatMessagesByDB)
            {
                SaveChatMessages(new List<string> { kvp.Key }, kvp.Value);
            }

            AddTagCRM(tags, configChat);
            SendMessIC(rootChatBots, configChat);
        }
        private Dictionary<string, List<string>> PrepareDbPaths(List<string> senderIds, string tenantId, bool isToday)
        {
            var result = new Dictionary<string, List<string>>();
            string basePath = Path.Combine(DataPath.DataBaseChatBot, tenantId);
            string dirCurrent = Directory.GetCurrentDirectory();
            DateTime date = isToday ? DateTime.Now : DateTime.Now.AddDays(-1);
            var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
            if (configChat == null)
            {
                return new Dictionary<string, List<string>>();
            }

            foreach (var senderId in senderIds)
            {
                var dbName = ConsistentHashing.GetChatBotDB(senderId);
                var fullPath = Path.Combine(dirCurrent, _commonData.GetFilePath(basePath, date), dbName);
                var paths = ConvertPathDB(fullPath, date, configChat);
                result[senderId] = paths;
            }

            return result;
        }
        private void SaveChatMessages(List<string> filePathTodays, List<ChatMessageDaily> messages)
        {
            if (messages == null || messages.Count == 0) return;

            var filePath = filePathTodays.FirstOrDefault();
            if (string.IsNullOrEmpty(filePath)) return;

            var dbPath = _createDB.CreateDBSQLiteHistorySizeSTT(filePath);

            try
            {
                using var dbContextToday = _contextFactory.CreateContext(dbPath);
                dbContextToday.Context.ChatMessageDailies.AddRange(messages);
                dbContextToday.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"Lỗi lưu ChatMessage (batch): {ex.Message}");

                try
                {
                    using var dbContextToday = _contextFactory.CreateContext(dbPath);
                    foreach (var message in messages)
                    {
                        try
                        {
                            dbContextToday.Context.ChatMessageDailies.Add(message);
                            dbContextToday.Context.SaveChanges();
                        }
                        catch (DbUpdateException exItem)
                        {
                            Log.Error($"Lỗi lưu từng ChatMessage: {exItem.Message}");
                        }
                    }
                }
                catch (Exception finalEx)
                {
                    Log.Error($"Fallback lưu toàn bộ ChatMessage thất bại: {finalEx.Message}");
                }
            }
        }
        private ChatSessionDaily? GetActiveSessionFromMultipleDBs(string senderId, List<string> dbPaths)
        {
            ChatSessionDaily? result = null;
            object lockObj = new object();

            Parallel.ForEach(dbPaths, (dbPath, state) =>
            {
                if (!File.Exists(dbPath)) return;

                using var dbContext = _contextFactory.CreateContext(dbPath);
                var session = dbContext.Context.ChatSessionDailies
                    .AsNoTracking()
                    .FirstOrDefault(x => !x.IsClose && x.SenderId == senderId);

                if (session != null)
                {
                    lock (lockObj)
                    {
                        if (result == null)
                        {
                            result = session;
                            state.Stop();
                        }
                    }
                }
            });

            return result;
        }
        private Dictionary<string, ChatSessionDaily> GetActiveSessionsFromMultipleDBs(Dictionary<string, List<string>> senderIdToDbPaths)
        {
            var result = new ConcurrentDictionary<string, ChatSessionDaily>();

            Parallel.ForEach(senderIdToDbPaths, senderEntry =>
            {
                var senderId = senderEntry.Key;
                var dbPaths = senderEntry.Value;

                foreach (var dbPath in dbPaths)
                {
                    if (!File.Exists(dbPath)) continue;

                    using var dbContext = _contextFactory.CreateContext(dbPath);
                    var session = dbContext.Context.ChatSessionDailies
                        .AsNoTracking()
                        .FirstOrDefault(x => !x.IsClose && x.SenderId == senderId);

                    if (session != null)
                    {
                        // Ưu tiên DB đầu tiên có session
                        if (result.TryAdd(senderId, session))
                            break; // đã có session thì không cần tiếp tục DB khác
                    }
                }
            });

            return result.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private List<ChatMessageDaily> GetActiveMessagesFromMultipleDBs(List<string> dbPaths, string sessionId)
        {
            var messageList = new List<ChatMessageDaily>();

            foreach (var dbPath in dbPaths)
            {
                if (!File.Exists(dbPath)) continue;

                using var dbContext = _contextFactory.CreateContext(dbPath);
                var messages = dbContext.Context.ChatMessageDailies
                    .Where(m => m.SessionId == sessionId && m.IsInputIC != true)
                    .ToList();

                if (messages.Count > 0)
                {
                    foreach (var msg in messages)
                    {
                        msg.IsInputIC = true;
                    }

                    dbContext.SaveChanges();
                    messageList.AddRange(messages);
                }
            }

            return messageList;
        }

        private List<ChatSessionDaily> GetActiveSenderFromMultipleDBs(List<string> dbPathList)
        {
            var results = new ConcurrentBag<ChatSessionDaily>();
            Parallel.ForEach(dbPathList, new ParallelOptions { MaxDegreeOfParallelism = 10 }, dbPath =>
            {
                try
                {
                    if (File.Exists(dbPath))
                    {
                        using var dbContext = _contextFactory.CreateContext(dbPath);
                        var openSessions = dbContext.Context.ChatSessionDailies
                            .AsNoTracking()
                            .Where(x => !x.IsClose)
                            .ToList();

                        foreach (var session in openSessions)
                        {
                            results.Add(session);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"GetActiveSenderFromMultipleDBs: {ex.Message}");
                }
            });

            var uniqueSessions = results
               .GroupBy(x => x.SenderId)
               .Select(g => g.OrderByDescending(x => x.DateMessageLast).First())
               .ToList();

            return uniqueSessions;
        }

        private List<CB_Customer> CreateInteractionCRM(List<CB_Customer> customersNew, List<string> filePathTodays, List<ChatSessionDaily> sessionDailiesNew, CB_ConfigChat configChat)
        {
            if (sessionDailiesNew == null || sessionDailiesNew.Count == 0) return customersNew;

            var filePath = filePathTodays.FirstOrDefault();
            if (string.IsNullOrEmpty(filePath)) return customersNew;

            var dbPath = _createDB.CreateDBSQLiteHistorySizeSTT(filePath);

            try
            {
                using var dbContextToday = _contextFactory.CreateContext(dbPath);

                foreach (var session in sessionDailiesNew)
                {
                    try
                    {
                        //Tạo kiểm tra và tạo interaction, Contact 
                        var (result, _) = _executeWF.Execute(configChat.WF_InteractionCRM, JsonConvert.SerializeObject(session), configChat.tenant_id);
                        if (result != null)
                        {
                            var response = JsonConvert.DeserializeObject<ResponseCreateCRM>(result.ToString());
                            session.InteractionCRM = response?.data ?? "";
                            //session.InteractionCRM = response?.InteractionId ?? "";
                            session.ContactId = response?.ContactId ?? "";
                            session.SessionId = response?.SessionId ?? session.SessionId ?? "";
                            customersNew.Where(c => c.SenderId == session.SenderId)
                                              .ToList()
                                              .ForEach(c => c.ContactId = session.ContactId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Create InteractionId for session {session.SessionId}: {ex.Message}");
                    }
                }

                dbContextToday.Context.ChatSessionDailies.AddRange(sessionDailiesNew);
                dbContextToday.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"Lỗi khi lưu ChatSessionDailies batch: {ex.Message}");

                try
                {
                    using var dbContextToday = _contextFactory.CreateContext(dbPath);
                    foreach (var session in sessionDailiesNew)
                    {
                        try
                        {
                            dbContextToday.Context.ChatSessionDailies.Add(session);
                            dbContextToday.Context.SaveChanges();
                        }
                        catch (DbUpdateException exItem)
                        {
                            Log.Error($"CreateInteractionCRM fallback từng session: {exItem.Message}");
                        }
                    }
                }
                catch (Exception finalEx)
                {
                    Log.Error($"Fallback CreateInteractionCRM toàn bộ thất bại: {finalEx.Message}");
                }
            }

            return customersNew;
        }

        private void AddTagCRM(TagModel tags, CB_ConfigChat configChat)
        {
            if (tags != null && tags.Data != null && tags.Data.Count > 0)
            {
                try
                {
                    Log.Information(JsonConvert.SerializeObject(tags));
                    _executeWF.Execute(configChat.Ev_Tag, JsonConvert.SerializeObject(tags), configChat.tenant_id);
                }
                catch (Exception ex)
                {
                    Log.Error($"Đẩy tin nhắn gặp user_support: {ex.Message}");
                }
            }
        }
        private void SendMessIC(List<ChatBotMessage.RootChatBot> rootChatBots, CB_ConfigChat configChat)
        {
            foreach (var rootChat in rootChatBots)
            {
                try
                {
                    var result1 = _executeWF.Execute(configChat.WF_SendChatIC, JsonConvert.SerializeObject(rootChat), configChat.tenant_id);
                }
                catch (Exception ex)
                {
                    Log.Error($"Đẩy 1 tin nhắn khi gặp user_support {ex.Message} \n bodyData {JsonConvert.SerializeObject(rootChat)}");
                }
            }
        }
        public List<string> ConvertPathDB(string path, DateTime dateTime, CB_ConfigChat configChat)
        {
            try
            {
                path = path.Replace("\\", "/");
                if (File.Exists(path))
                {
                    var directory = Path.GetDirectoryName(path);
                    var filePrefix = Path.GetFileNameWithoutExtension(path);

                    return Directory
                        .GetFiles(directory, $"{filePrefix}*.db")
                        .OrderByDescending(File.GetLastWriteTime)
                        .ToList();
                }

                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path).ToList();
                    return files;
                }

                // Trường hợp còn lại: không phải file, không phải thư mục => có thể là file zip cần giải nén
                return ExtractFilesFromZip(path, dateTime, configChat);
            }
            catch (Exception ex)
            {
                Log.Error($"ConvertPathDB: {ex.Message} \n path: {path}");
                return new List<string>();
            }
        }
        private List<string> ExtractFilesFromZip(string dbPath, DateTime dateTime, CB_ConfigChat configChat)
        {
            try
            {
                string zipFilePath = GetZipFilePathForDate(dateTime, configChat);  // Hàm này cần lấy đường dẫn file zip tương ứng với ngày
                string extractPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryFolderZip, configChat.tenant_id);

                if (!File.Exists(zipFilePath))
                {
                    Log.Warning($"Zip file {zipFilePath} không tồn tại.");
                    return new List<string>();
                }

                _commonData.ExtractFileZipPattern(zipFilePath, extractPath, $"^{Path.GetFileNameWithoutExtension(dbPath)}(_\\d+)?\\.db$");

                // Lấy các file DB đã giải nén
                return Directory.GetFiles(extractPath, $"{Path.GetFileNameWithoutExtension(dbPath)}*.db")
                       .OrderByDescending(f => File.GetLastWriteTime(f))
                       .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"ExtractFilesFromZip: {ex.Message}");
                return new List<string>();
            }
        }
        private string GetZipFilePathForDate(DateTime dateTime, CB_ConfigChat configChat)
        {
            string dateInput = dateTime.ToString("yyyyMMdd");
            string zipFileName = $"BackupChatBot_{dateInput}.zip";
            string zipFolder = Path.Combine(Directory.GetCurrentDirectory(), configChat.PathBackUp, configChat.tenant_id);
            return Path.Combine(zipFolder, zipFileName);
        }

        public void InputDataSupportIC(CB_ConfigChat configChat, ChatSessionDaily chatSessionDaily, string name)
        {
            DateTimeOffset dto = new DateTimeOffset(DateTime.Now);
            long timestamp = dto.ToUnixTimeMilliseconds();

            var rootChatBot = new ChatBotMessage.RootChatBot()
            {
                timestamp = timestamp,
                bot_code = configChat.IdBOT,
                @event = "user_sent_message",
                data = new ChatBotMessage.Data()
                {
                    sender = new ChatBotMessage.Sender()
                    {
                        id = chatSessionDaily.SenderId,
                        name = name
                    },
                    channel = chatSessionDaily.Channel,
                    sub_channel = chatSessionDaily.SubChannel,
                    message = new Models.CreateFile.ChatBotMessage.Message()
                    {
                        type = "text",
                        content = new Models.CreateFile.ChatBotMessage.Content()
                        {
                            text = "KH yêu cầu gặp tư vấn viên"
                        }
                    },
                    session_id = chatSessionDaily.SessionId
                }
            };

            Log.Information($"Đẩy tin nhắn vào support IC: {JsonConvert.SerializeObject(rootChatBot)}");
            try
            {
                var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext1 = tContext1.GetTenantContext(configChat.tenant_id).Context;
                this._executeWF.SetTenantContext(tenantContext1);
                var result1 = _executeWF.Execute(configChat.WF_SendChatIC, JsonConvert.SerializeObject(rootChatBot), configChat.tenant_id);
                tenantContext1.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Đẩy 1 tin nhắn khi gặp user_support {ex.Message} \n bodyData {JsonConvert.SerializeObject(rootChatBot)}");
            }
        }

        /// <summary>
        /// Khi IC tạo session đẩy toàn bộ tin nhắn sang IC và gửi định danh
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="senderId"></param>
        public void CreateSessionIC(string tenantId, string user_social_id, string customerId, string idIc)
        {
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChat == null)
                {
                    Log.Error($"CreateSessionIC configChat is null tenantId: {tenantId}, user_social_id: {user_social_id}");
                    return;
                }

                var splitUserSocial = user_social_id.Split('_').ToList();

                if (splitUserSocial == null || splitUserSocial.Count == 0)
                {
                    return;
                }

                string senderId = string.Join("_", splitUserSocial.Skip(1));
                string channel = splitUserSocial[0];

                #region Định danh IC
                InfoCustomer infoCustomer = new InfoCustomer();
                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(tenantId).Context;
                this._executeWF.SetTenantContext(tenantContext);

                CB_Customer? customer = tenantContext.Context.CB_Customers.FirstOrDefault(ptr => ptr.SenderId == senderId);

                if (customer != null)
                {
                    var dbName = ConsistentHashing.GetChatBotDB(senderId);
                    var dbPathsTodayBySenderId = new Dictionary<string, List<string>>();
                    var dbPathsYesterdayBySenderId = new Dictionary<string, List<string>>();

                    string basePath = Path.Combine(DataPath.DataBaseChatBot, configChat.tenant_id);
                    string dirCurrent = Directory.GetCurrentDirectory();
                    var fullPathToday = Path.Combine(dirCurrent, _commonData.GetFilePath(basePath, DateTime.Now), dbName);
                    var fullPathYesterday = Path.Combine(dirCurrent, _commonData.GetFilePath(basePath, DateTime.Now.AddDays(-1)), dbName);
                    dbPathsTodayBySenderId[senderId] = ConvertPathDB(fullPathToday, DateTime.Now, configChat);
                    dbPathsYesterdayBySenderId[senderId] = ConvertPathDB(fullPathYesterday, DateTime.Now.AddDays(-1), configChat);
                    List<string> senderIds = new List<string>()
                    {
                        senderId
                    };

                    var dbPathsTodayDict = PrepareDbPaths(senderIds, configChat.tenant_id, true);
                    var dbPathsYesterdayDict = PrepareDbPaths(senderIds, configChat.tenant_id, false);

                    var activeSessionsToday = GetActiveSessionsFromMultipleDBs(dbPathsTodayDict);
                    Dictionary<string, ChatSessionDaily> activeSessions = activeSessionsToday;
                    if (activeSessionsToday == null || activeSessionsToday.Count == 0)
                    {
                        var activeSessionsYesterday = GetActiveSessionsFromMultipleDBs(dbPathsYesterdayDict);
                        activeSessions = activeSessionsYesterday;
                    }

                    if (activeSessions == null || activeSessions.Count == 0)
                    {
                        return;
                    }

                    //Đẩy vào IC interaction và contact
                    var chatSessionList = activeSessions.Values.FirstOrDefault();
                    if (chatSessionList == null)
                    {
                        return;
                    }

                    infoCustomer = new InfoCustomer()
                    {
                        phoneNo = customer.Phone,
                        cif_list_data = customer.cif_list_data,
                        customerInfo = customer.CustomerInfo,
                        sender_id = senderId,
                        channel = channel,
                        customerId = customerId,
                        idIc = idIc,
                        sessionId = chatSessionList.SessionId ?? "",
                        interactionId = chatSessionList.InteractionCRM ?? "",
                        contactId = customer.ContactId ?? "",
                        sub_channel = chatSessionList.SubChannel ?? "",
                        customerName = chatSessionList.CustomerName ?? "",
                    };

                    try
                    {
                        //Todo: Call WF định danh vào IC trả ra contactId để gán vào ContactId
                        var result1 = _executeWF.Execute(configChat.Ev_User_Support, JsonConvert.SerializeObject(infoCustomer), configChat.tenant_id);

                        ResponseCreateCRM responseCreateCRM = JsonConvert.DeserializeObject<ResponseCreateCRM>(JsonConvert.SerializeObject(result1));
                        if (string.IsNullOrEmpty(customer.ContactId))
                        {
                            customer.ContactId = responseCreateCRM.ContactId;
                            tenantContext.Context.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Ev_User_Support: {ex.Message}");
                    }

                    #region Đẩy tất cả tin nhắn vào IC
                    try
                    {
                        var todayPaths = dbPathsTodayDict.TryGetValue(senderId, out var todayList) ? todayList : new List<string>();
                        var yesterdayPaths = dbPathsYesterdayDict.TryGetValue(senderId, out var yesterdayList) ? yesterdayList : new List<string>();
                        var messageChatsToday = GetActiveMessagesFromMultipleDBs(todayPaths, chatSessionList.SessionId);
                        var messageChatsYesterday = GetActiveMessagesFromMultipleDBs(yesterdayPaths, chatSessionList.SessionId);

                        InputDataIC(configChat, tenantId, senderId, chatSessionList, messageChatsYesterday, messageChatsToday);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Task.Run InputDataIC exception: {ex.Message}");
                    }
                    #endregion Đẩy tất cả tin nhắn vào IC
                }
                #endregion Định danh IC
            }
            catch (Exception ex)
            {
                Log.Error($"CreateSessionIC: {ex.Message} \n tenantId: {tenantId}, user_social_id: {user_social_id}, customerId: {customerId}, idIc: {idIc}");
            }
        }

        public void InputDataIC(CB_ConfigChat configChat, string tenantId, string senderId, ChatSessionDaily chatSessionDaily,
            List<ChatMessageDaily> yesterdayChatMessage, List<ChatMessageDaily> todayChatMessage)
        {
            try
            {
                var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext = tContext.GetTenantContext(tenantId).Context;
                this._executeWF.SetTenantContext(tenantContext);
                for (int i = 0; i < yesterdayChatMessage.Count; i++)
                {
                    try
                    {
                        var result = _executeWF.Execute(configChat.WF_SendChatIC, yesterdayChatMessage[i].MessageContent, configChat.tenant_id);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Day toan bo tin nhăn khi gap user_support yesterday: {ex.Message} \n getChatMessage: {JsonConvert.SerializeObject(yesterdayChatMessage[i])}");
                    }
                }

                for (int i = 0; i < todayChatMessage.Count; i++)
                {
                    try
                    {
                        var result = _executeWF.Execute(configChat.WF_SendChatIC, todayChatMessage[i].MessageContent, configChat.tenant_id);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Day toan bo tin nhăn khi gap user_support today: {ex.Message} \n getChatMessage: {JsonConvert.SerializeObject(todayChatMessage[i])}");
                    }
                }

                tenantContext.Context.Dispose();

            }
            catch (Exception ex)
            {
                Log.Error($"InputDataIC: {ex.Message} \n tenantId: {tenantId},  senderId: {senderId}");
            }
        }

        public void IdentificationCustomer(CB_ConfigChat configChat)
        {
            string newFilePath = "";
            string filePathOld = "";
            try
            {
                if (configChat == null)
                {
                    return;
                }

                string content = "";

                string path = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryFolderCustomer, configChat.tenant_id);

                if (!Directory.Exists(path) || !Directory.GetFiles(path).Any())
                {
                    return;
                }

                var mostRecentFile = Directory.GetFiles(path)
                    .Select(file => new FileInfo(file))
                    .OrderBy(fileInfo => fileInfo.LastWriteTime)
                    .FirstOrDefault();

                int valueSecond = _commonData.ShowSecond();
                if (mostRecentFile == null || mostRecentFile.Name.Contains(DateTime.Now.ToString("yyyyMMddHHmm") + valueSecond))
                {
                    return;
                }

                string newFolderPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolderCustomer);
                _commonData.CreateFolder1(newFolderPath);

                newFilePath = Path.Combine(newFolderPath, mostRecentFile.Name);
                filePathOld = mostRecentFile.FullName;
                File.Move(mostRecentFile.FullName, newFilePath);
                if (File.Exists(mostRecentFile.FullName) && File.Exists(newFilePath))
                {
                    try
                    {
                        File.Delete(mostRecentFile.FullName);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Delete {ex.Message} \n path: {mostRecentFile.FullName}");
                    }
                }

                using (var reader = new StreamReader(newFilePath))
                {
                    content = reader.ReadToEnd().TrimEnd(',');
                }

                content = $"{{\"Data\":[{content}]}}";

                DataFileCustomer? dataFileCustomer = JsonConvert.DeserializeObject<DataFileCustomer>(content);
                if (dataFileCustomer == null || dataFileCustomer.Data == null)
                {
                    return;
                }

                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(configChat.tenant_id).Context;
                _executeWF.SetTenantContext(tenantContext);

                var senderIds = dataFileCustomer.Data
                 .Select(g => g.sender_id)
                 .Distinct()
                 .ToList();

                var customerDict = tenantContext.Context.CB_Customers
                    .AsNoTracking()
                    .Where(c => senderIds.Contains(c.SenderId))
                    .ToDictionary(c => c.SenderId, c => c.ContactId);

                var mergedCustomers = dataFileCustomer.Data
                    .GroupBy(x => x.sender_id)
                    .Select(group =>
                    {
                        var merged = new InfoCustomer { sender_id = group.Key };

                        foreach (var item in group)
                        {
                            if (string.IsNullOrEmpty(merged.channel)) merged.channel = item.channel;
                            if (string.IsNullOrEmpty(merged.cif_list_cached)) merged.cif_list_cached = item.cif_list_cached;
                            if (string.IsNullOrEmpty(merged.cif_list_created_date)) merged.cif_list_created_date = item.cif_list_created_date;
                            if (string.IsNullOrEmpty(merged.cif_list_data)) merged.cif_list_data = item.cif_list_data;
                            if (string.IsNullOrEmpty(merged.cif_list_error)) merged.cif_list_error = item.cif_list_error;
                            if (string.IsNullOrEmpty(merged.cif_list_error_message)) merged.cif_list_error_message = item.cif_list_error_message;
                            if (string.IsNullOrEmpty(merged.cif_list_request_id)) merged.cif_list_request_id = item.cif_list_request_id;
                            if (string.IsNullOrEmpty(merged.customerInfo)) merged.customerInfo = item.customerInfo;
                            if (string.IsNullOrEmpty(merged.email)) merged.email = item.email;
                            if (string.IsNullOrEmpty(merged.phoneNo)) merged.phoneNo = item.phoneNo;
                            if (string.IsNullOrEmpty(merged.sender_name)) merged.sender_name = item.sender_name;
                            if (string.IsNullOrEmpty(merged.sub_channel)) merged.sub_channel = item.sub_channel;
                            if (string.IsNullOrEmpty(merged.tenant_id)) merged.tenant_id = item.tenant_id;
                            if (string.IsNullOrEmpty(merged.customerId)) merged.customerId = item.customerId;
                            if (string.IsNullOrEmpty(merged.idIc)) merged.idIc = item.idIc;
                            if (string.IsNullOrEmpty(merged.contactId)) merged.contactId = item.contactId;
                            if (string.IsNullOrEmpty(merged.sessionId)) merged.sessionId = item.sessionId;
                            if (string.IsNullOrEmpty(merged.interactionId)) merged.interactionId = item.interactionId;
                            if (string.IsNullOrEmpty(merged.customerName)) merged.customerName = item.customerName;
                        }

                        if (customerDict.TryGetValue(group.Key, out var contactIdFromDb))
                        {
                            merged.contactId ??= contactIdFromDb;
                        }

                        return merged;
                    })
                    .ToList();

                List<CB_Customer> cB_Customers = new List<CB_Customer>();
                foreach (var customerInfo in mergedCustomers)
                {
                    try
                    {
                        ResponseCreateCRM responseCreateCRM = IdentificationCRM(configChat, customerInfo);
                        //Update và insert 
                        cB_Customers.Add(new CB_Customer()
                        {
                            cif_list_data = customerInfo.cif_list_data != "<nil>" && !string.IsNullOrEmpty(customerInfo.cif_list_data) ? customerInfo.cif_list_data : "",
                            ContactId = responseCreateCRM.ContactId,
                            CustomerInfo = customerInfo.customerInfo,
                            Phone = customerInfo.phoneNo,
                            SenderId = customerInfo.sender_id,
                            IsDelete = false,
                            CreatedDate = DateTime.Now,
                            ModificationDate = DateTime.Now,
                            Id = Guid.NewGuid().ToString()
                        });

                        var objSessionChat = SessionChatBot.GetSession(customerInfo.sender_id);
                        if (objSessionChat != null && objSessionChat.IsSupport == true)
                        {
                            try
                            {
                                InfoCustomer infoCustomer1 = new InfoCustomer()
                                {
                                    phoneNo = customerInfo.phoneNo,
                                    cif_list_data = customerInfo.cif_list_data != "<nil>" && !string.IsNullOrEmpty(customerInfo.cif_list_data) ? customerInfo.cif_list_data : "",
                                    customerInfo = customerInfo.customerInfo,
                                    sender_id = customerInfo.sender_id,
                                    channel = customerInfo.channel,
                                    contactId = responseCreateCRM?.data ?? "",
                                    customerName = customerInfo?.customerName ?? "",
                                    sub_channel = customerInfo?.sub_channel ?? ""
                                };

                                try
                                {
                                    if (!string.IsNullOrEmpty(configChat.Ev_User_Support))
                                    {
                                        var result1 = _executeWF.Execute(configChat.Ev_User_Support, JsonConvert.SerializeObject(infoCustomer1), configChat.tenant_id);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Định danh đẩy vào IC khi gặp support từ session {ex.Message} \n infoCustomer: {JsonConvert.SerializeObject(infoCustomer1)}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"infoCustomer {ex.Message} \n infoCustomer {JsonConvert.SerializeObject(customerInfo)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"IdentificationCRM: {ex.Message}");
                    }
                }

                //Update vào db
                var (columns, values) = ExtractColumnsAndValues(cB_Customers);
                ExecuteMerge(
                    configChat.tenant_id,
                    "CB_Customers",
                    "SenderId",
                    columns,
                    values
                );

                //InsertOrUpdateCustomersWithRetry(cB_Customers, tenantContext);
                File.Delete(newFilePath);
                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"IdentificationCustomer: {ex.Message}");
                if (File.Exists(filePathOld) && File.Exists(newFilePath))
                {
                    try
                    {
                        File.Delete(newFilePath);
                    }
                    catch (Exception ex1)
                    {
                        Log.Error($"Delete {ex1.Message} \n path: {newFilePath}");
                    }
                }
            }
        }

        public void InsertOrUpdateCustomersWithRetry(List<CB_Customer> cB_Customers, TenantContext tenantContext, int maxRetryCount = 3, int delayMilliseconds = 1000)
        {
            int attempt = 0;
            bool success = false;

            while (attempt < maxRetryCount && !success)
            {
                try
                {
                    using (var conn = tenantContext.Context.Database.GetDbConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        using (var command = conn.CreateCommand())
                        {
                            command.Transaction = transaction;


                            var query = new StringBuilder(@"
                                MERGE INTO CB_Customers WITH (UPDLOCK) AS target
                                USING (
                                    VALUES 
                            ");

                            var valueRows = new List<string>();

                            foreach (var record in cB_Customers)
                            {
                                var id = Guid.NewGuid();
                                var createdDate = record.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                var modifiedDate = record.ModificationDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                var isDelete = record.IsDelete ? 1 : 0;

                                valueRows.Add(string.Format("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', {8})",
                                    id,
                                    Escape(record.SenderId),
                                    Escape(record.CustomerInfo),
                                    Escape(record.cif_list_data),
                                    Escape(record.Phone),
                                    Escape(record.ContactId),
                                    createdDate,
                                    modifiedDate,
                                    isDelete));
                            }

                            query.AppendJoin(",\n", valueRows);

                            query.Append(@"
                            ) AS source (
                                Id,
                                SenderId,
                                CustomerInfo,
                                cif_list_data,
                                Phone,
                                ContactId,
                                CreatedDate,
                                ModificationDate,
                                IsDelete
                            )
                            ON target.SenderId = source.SenderId
                            WHEN MATCHED THEN
                                UPDATE SET
                                    CustomerInfo      = COALESCE(NULLIF(source.CustomerInfo, ''), target.CustomerInfo),
                                    cif_list_data     = COALESCE(NULLIF(source.cif_list_data, ''), target.cif_list_data),
                                    Phone             = COALESCE(NULLIF(source.Phone, ''), target.Phone),
                                    ContactId         = COALESCE(NULLIF(source.ContactId, ''), target.ContactId),
                                    ModificationDate  = COALESCE(source.ModificationDate, target.ModificationDate),
                                    IsDelete          = COALESCE(source.IsDelete, target.IsDelete)
                            WHEN NOT MATCHED THEN
                                INSERT (
                                    Id,
                                    SenderId,
                                    CustomerInfo,
                                    cif_list_data,
                                    Phone,
                                    ContactId,
                                    CreatedDate,
                                    ModificationDate,
                                    IsDelete
                                )
                                VALUES (
                                    source.Id,
                                    source.SenderId,
                                    source.CustomerInfo,
                                    source.cif_list_data,
                                    source.Phone,
                                    source.ContactId,
                                    source.CreatedDate,
                                    source.ModificationDate,
                                    source.IsDelete
                                );");

                            command.CommandText = query.ToString();
                            command.ExecuteNonQuery();
                            transaction.Commit();

                            success = true;
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    attempt++;
                    if (attempt < maxRetryCount)
                    {
                        Thread.Sleep(delayMilliseconds);
                    }
                    else
                    {
                        Log.Error($"Retry limit reached. Deadlock or other error occurred. {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while processing the MERGE operation. {ex.Message}");
                }
            }
        }
        public (List<string> Columns, List<List<object>> Values) ExtractColumnsAndValues<T>(List<T> entities, params string[] excludeProperties)
        {
            var columns = new List<string>();
            var values = new List<List<object>>();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !excludeProperties.Contains(p.Name)).ToList();

            columns = properties.Select(p => p.Name).ToList();

            foreach (var entity in entities)
            {
                var row = new List<object>();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(entity);
                    row.Add(value);
                }
                values.Add(row);
            }

            return (columns, values);
        }

        public void ExecuteMerge(
            string tenantId,
            string tableName,
            string keyColumn,
            List<string> columns,
            List<List<object>> values,
            int retryCount = 3,
            int delayMilliseconds = 2000)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;
            var columnList = string.Join(", ", columns);
            var sourceColumns = columns;
            int attempt = 0;
            while (attempt < retryCount)
            {
                try
                {
                    using (var conn = tenantContext.Context.Database.GetDbConnection())
                    {
                        conn.Open();
                        using (var transaction = conn.BeginTransaction())
                        using (var command = conn.CreateCommand())
                        {
                            command.Transaction = transaction;
                            var query = new StringBuilder();
                            query.AppendLine($"MERGE INTO [{tableName}] WITH (UPDLOCK) AS target");
                            query.AppendLine("USING (VALUES");
                            var rowStrings = values.Select(row =>
                            {
                                var valStrs = row.Select(v =>
                                {
                                    return v switch
                                    {
                                        null => "NULL",
                                        string s => $"'{s.Replace("'", "''")}'",
                                        bool b => b ? "1" : "0",
                                        DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.fff}'",
                                        _ => $"'{v}'"
                                    };
                                });
                                return $"({string.Join(", ", valStrs)})";
                            });

                            query.AppendLine(string.Join(",\n", rowStrings));
                            query.AppendLine(") AS source (" + columnList + ")");
                            query.AppendLine($"ON target.[{keyColumn}] = source.[{keyColumn}]");
                            query.AppendLine("WHEN MATCHED THEN UPDATE SET");

                            var updateSet = columns
                                .Where(c => !string.Equals(c, keyColumn, StringComparison.OrdinalIgnoreCase))
                                .Select(c =>
                                    $"{c} = COALESCE(NULLIF(source.{c}, ''), target.{c})");

                            query.AppendLine(string.Join(",\n", updateSet));
                            query.AppendLine("WHEN NOT MATCHED THEN");
                            query.AppendLine($"INSERT ({columnList})");
                            query.AppendLine($"VALUES ({string.Join(", ", sourceColumns.Select(c => $"source.{c}"))});");
                            command.CommandText = query.ToString();
                            command.ExecuteNonQuery();
                            transaction.Commit();
                            tenantContext.Context.Dispose();
                            break;
                        }
                    }
                }
                catch (SqlException ex) when (ex.Number == 1205)
                {
                    attempt++;
                    if (attempt < retryCount)
                    {
                        Thread.Sleep(delayMilliseconds);
                    }
                    else
                    {
                        Log.Error($"Retry limit reached. Deadlock or other error occurred. {ex.Message}");
                        tenantContext.Context.Dispose();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while processing the MERGE operation. {ex.Message}");
                    tenantContext.Context.Dispose();
                    return;
                }
            }
        }

        private static string Escape(string? value)
        {
            return (value ?? "").Replace("'", "''");
        }
        public void RemoveSessionId(string senderId)
        {
            try
            {
                SessionChatBot.RemoveSession(senderId);
            }
            catch (Exception ex)
            {
                Log.Error($"RemoveSessionId {ex.Message} senderId: {senderId}");
            }
        }

        public SessionObj CheckAndCreateSession(string tenantId, string senderId)
        {
            try
            {
                var configChatBots = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                if (configChatBots == null)
                {
                    return null;
                }

                if (!SessionChatBot.HasAnySession())
                {
                    try
                    {
                        var sessions = new List<KeyValuePair<string, SessionObj>>();
                        string basePath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                        var datesToCheck = new[] { DateTime.Now, DateTime.Now.AddDays(-1) };

                        foreach (var date in datesToCheck)
                        {
                            string filePath = _commonData.GetFilePath(basePath, date);
                            string dbPath = Path.Combine(filePath, "chatbot.db");

                            if (File.Exists(dbPath))
                            {
                                using var dbContext = _contextFactory.CreateContext(dbPath);
                                var chatSessions = dbContext.Context.ChatSessionDailies
                                    .Where(ptr => !ptr.IsClose && ptr.IsSupport)
                                    .Select(ptr => new KeyValuePair<string, SessionObj>(
                                        ptr.SenderId,
                                        new SessionObj
                                        {
                                            SessionId = ptr.SessionId,
                                            CreateDate = DateTime.Now,
                                            ThisSite = "",
                                            IsSupport = ptr.IsSupport
                                        }))
                                    .ToList();

                                sessions.AddRange(chatSessions);
                            }
                        }

                        if (sessions.Any())
                        {
                            foreach (var kvp in sessions)
                            {
                                SessionChatBot.AddOrUpdateSession(kvp.Key, kvp.Value, Int32.Parse(configChatBots.EndChatTime));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"CheckAndCreateSession hasSessions: {ex.Message}");
                    }
                }

                var session = SessionChatBot.GetSession(senderId);
                if (string.IsNullOrEmpty(session?.SessionId))
                {
                    string sessionId = Guid.NewGuid().ToString();
                    string thisSite = IBGlobalConfig.ThisSite;

                    var urlLogService = IBGlobalConfig.LogServiceIBox.FirstOrDefault(ptr => !ptr.Contains(thisSite));

                    if (urlLogService != null)
                    {
                        var result = _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = "{}",
                            Headers = new List<RestAPIHeader>
                            {
                                new RestAPIHeader { Label = "Content-Type", Value = "application/json" }
                            },
                            Method = "GET",
                            Timeout = 30,
                            Url = $"{urlLogService}/api/SaveDataChatBot/CheckAndGenSession/{senderId}"
                        });

                        if (!string.IsNullOrEmpty(result?.Result))
                        {
                            var apiSession = JsonConvert.DeserializeObject<SessionObj>(result.Result);
                            if (apiSession != null)
                            {
                                var sessionFromAPI = new SessionObj
                                {
                                    SessionId = apiSession.SessionId,
                                    ThisSite = string.IsNullOrEmpty(apiSession.ThisSite) ? thisSite : apiSession.ThisSite,
                                    CreateDate = DateTime.Now,
                                    IsSupport = apiSession.IsSupport
                                };

                                SessionChatBot.AddOrUpdateSession(senderId, sessionFromAPI, configChatBots == null ? 12 : Int32.Parse(configChatBots.EndChatTime));
                                return sessionFromAPI;
                            }
                        }
                    }

                    var newSession = new SessionObj
                    {
                        SessionId = sessionId,
                        ThisSite = thisSite,
                        CreateDate = DateTime.Now,
                        IsSupport = true
                    };

                    SessionChatBot.AddOrUpdateSession(senderId, newSession, configChatBots == null ? 12 : Int32.Parse(configChatBots.EndChatTime));
                    return newSession;
                }

                return session;
            }
            catch (Exception ex)
            {
                Log.Error($"AddOrUpdateSession: {ex.Message} \n tenantId: {tenantId}, senderId: {senderId}");
                return new SessionObj();
            }
        }

        public SessionObj? CheckSession(string senderId, int check = 0)
        {
            try
            {
                if (string.IsNullOrEmpty(senderId))
                {
                    return new SessionObj();
                }

                SessionObj? sessionObj = SessionChatBot.GetSession(senderId);

                if (sessionObj == null)
                {
                    var urlLogServices = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                    foreach (var urlLog in urlLogServices)
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
                                    }
                                },
                            Method = "GET",
                            Timeout = 30,
                            Url = $"{urlLog}/api/SaveDataChatBot/GetSession?senderId={senderId}"
                        });

                        if (!string.IsNullOrEmpty(result.Result))
                        {
                            var session = JsonConvert.DeserializeObject<SessionObj>(result.Result);
                            if (session != null)
                            {
                                SessionChatBot.AddOrUpdateSession(senderId, session, 24);
                            }
                        }
                    }

                    sessionObj = SessionChatBot.GetSession(senderId);

                    if (sessionObj == null && check < 2)
                    {
                        check++;
                        return CheckSession(senderId, check);
                    }
                }

                return sessionObj;
            }
            catch (Exception ex)
            {
                Log.Error($"CheckSession: {ex.Message}");
                return new SessionObj();
            }
        }

        public void UpdateInteractionChat(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId)
        {
            try
            {
                var logServices = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                foreach (var urlLog in logServices)
                {
                    _restAPI.SendChatBot(new RestAPIRequest()
                    {
                        Body = JsonConvert.SerializeObject(requestUpdateInteractionChatBot),
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
                        Url = $"{urlLog}/api/SaveDataChatBot/UpdateInteractionChatSub/{tenantId}"
                    });
                }

                UpdateInteractionChatSub(requestUpdateInteractionChatBot, tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateInteractionChat: {ex.Message}");
            }
        }

        public void UpdateInteractionChatSub(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId)
        {
            try
            {
                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == tenantId);
                string pathCurrent = Path.Combine(DataPath.DataBaseChatBot, tenantId);
                string filePathYesterday = Path.Combine(Directory.GetCurrentDirectory(), _commonData.GetFilePath(pathCurrent, DateTime.Now.AddDays(-1)), "chatbot.db");
                string filePathToday = Path.Combine(Directory.GetCurrentDirectory(), _commonData.GetFilePath(pathCurrent, DateTime.Now), "chatbot.db");
                if (requestUpdateInteractionChatBot == null || requestUpdateInteractionChatBot.SessionId == null || string.IsNullOrEmpty(requestUpdateInteractionChatBot.SenderId))
                {
                    return;
                }

                ChatSessionDaily? chatSession = new ChatSessionDaily();

                using (var dbContextDaily = _contextFactory.CreateContext(filePathToday))
                {
                    var chatSession1 = dbContextDaily.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SessionId == requestUpdateInteractionChatBot.SessionId);

                    if (chatSession1 != null)
                    {
                        chatSession1.InteractionCRM = requestUpdateInteractionChatBot.InteractionId;
                        chatSession1.SessionId = string.IsNullOrEmpty(requestUpdateInteractionChatBot.SessionId) ? chatSession1.SessionId : requestUpdateInteractionChatBot.SessionId;
                        chatSession1.SenderId = string.IsNullOrEmpty(requestUpdateInteractionChatBot.SenderId) ? chatSession1.SenderId : requestUpdateInteractionChatBot.SenderId;
                        dbContextDaily.Context.SaveChangesAsync();
                        chatSession = chatSession1;
                    }
                    else
                    {
                        var urlLog = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                        foreach (var url in urlLog)
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
                                      }
                                },
                                Method = "GET",
                                Timeout = 30,
                                Url = $"{url}/api/SaveDataChatBot/GetChatSessionDaily/{tenantId}/{requestUpdateInteractionChatBot.SenderId}"
                            });

                            if (!string.IsNullOrEmpty(result.Result))
                            {
                                var chatSession2 = JsonConvert.DeserializeObject<ChatSessionDaily>(result.Result);
                                if (chatSession2 != null)
                                {
                                    chatSession2.Channel = string.IsNullOrEmpty(chatSession2.Channel) ? "livechat" : chatSession2.Channel;
                                    chatSession2.SessionId = string.IsNullOrEmpty(chatSession2.SessionId) ? requestUpdateInteractionChatBot.SessionId : chatSession2.SessionId;
                                    chatSession2.SenderId = string.IsNullOrEmpty(chatSession2.SenderId) ? requestUpdateInteractionChatBot.SenderId ?? "" : chatSession2.SenderId;
                                    chatSession2.InteractionCRM = requestUpdateInteractionChatBot?.InteractionId ?? "";
                                    dbContextDaily.Context.ChatSessionDailies.Add(chatSession2);
                                    dbContextDaily.Context.SaveChangesAsync();
                                    chatSession = chatSession2;
                                    break;
                                }
                            }
                            else
                            {
                                ChatSessionDaily chatSessionDaily = new ChatSessionDaily()
                                {
                                    SessionId = chatSession.SessionId,
                                    InteractionCRM = chatSession.InteractionCRM,
                                    SenderId = chatSession.SenderId,
                                    Channel = "livechat"
                                };
                                try
                                {
                                    dbContextDaily.Context.ChatSessionDailies.Add(chatSessionDaily);
                                    dbContextDaily.Context.SaveChangesAsync();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Save dbContextDaily ChatSessionDailies: {ex.Message}");
                                }
                            }
                        }

                        string senderId = requestUpdateInteractionChatBot?.SenderId ?? "";

                        var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

                        var tenantContext = context.GetTenantContext(tenantId).Context;

                        CB_Customer? customer = tenantContext.Context.CB_Customers.FirstOrDefault(ptr => ptr.SenderId == senderId);

                        this._executeWF.SetTenantContext(tenantContext);

                        if (customer != null)
                        {
                            RequestContactIDInteractionId requestContactIDInteractionId = new RequestContactIDInteractionId();
                            try
                            {
                                requestContactIDInteractionId = new RequestContactIDInteractionId()
                                {
                                    contactId = customer.ContactId,
                                    interactionId = requestUpdateInteractionChatBot?.InteractionId ?? "",
                                    senderId = senderId,
                                    sessionId = chatSession?.SessionId ?? "",
                                    channel = chatSession?.Channel ?? "",
                                    customerId = chatSession?.CustomerId ?? "",
                                    idIc = chatSession?.IdIc ?? "",
                                    customerName = chatSession?.CustomerName ?? "",
                                    phoneNo = customer.Phone ?? "",
                                    cif_list_data = !string.IsNullOrEmpty(customer.cif_list_data) && customer.cif_list_data != "<nil>" ? customer.cif_list_data : "",
                                    customerInfo = customer.CustomerInfo ?? "",
                                    subChannel = chatSession?.SubChannel ?? ""
                                };


                                if (configChat != null)
                                {
                                    try
                                    {
                                        var result = _executeWF.Execute(configChat.WF_UpdateIC_Interaction, JsonConvert.SerializeObject(requestContactIDInteractionId), tenantId);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"WF_UpdateIC_Interaction {ex.Message} \n requestContactIDInteractionId: {JsonConvert.SerializeObject(requestContactIDInteractionId)}");
                                    }

                                    try
                                    {
                                        SessionObj sessionObj = CheckSession(senderId);
                                        Models.CreateFile.CRM.Respone.ResponseCreateCRM responseCreateCRM = new ResponseCreateCRM();
                                        if (sessionObj != null && sessionObj.ThisSite == IBGlobalConfig.ThisSite && !string.IsNullOrEmpty(chatSession?.Channel) && chatSession?.Channel.ToUpper() == "FACEBOOK")
                                        {
                                            var result = _executeWF.Execute(configChat.WF_CustomerIdentification, JsonConvert.SerializeObject(requestContactIDInteractionId), configChat.tenant_id);


                                            responseCreateCRM = JsonConvert.DeserializeObject<Models.CreateFile.CRM.Respone.ResponseCreateCRM>(JsonConvert.SerializeObject(result));


                                            customer.ContactId = responseCreateCRM.data;

                                            tenantContext.Context.SaveChangesAsync();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"UpdateIC_Interaction WF_CustomerIdentification {ex.Message} \n requestContactIDInteractionId: {JsonConvert.SerializeObject(requestContactIDInteractionId)}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"WF_UpdateIC_Interaction {ex.Message} \n requestContactIDInteractionId: {JsonConvert.SerializeObject(requestContactIDInteractionId)}");
                            }
                        }

                        tenantContext.Context.Dispose();
                    }

                    if (chatSession == null)
                    {
                        using (var dbContextDailyYesterday = _contextFactory.CreateContext(filePathYesterday))
                        {

                            var chatSessionYesterday = dbContextDailyYesterday.Context.ChatSessionDailies.FirstOrDefault(ptr => ptr.SessionId == requestUpdateInteractionChatBot.SessionId);


                            if (chatSessionYesterday != null)
                            {
                                chatSessionYesterday.InteractionCRM = requestUpdateInteractionChatBot?.InteractionId ?? "";
                                dbContextDailyYesterday.Context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateInteractionChatSub: {ex.Message}");
            }
        }

        public void UpdateContactChat(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId)
        {
            try
            {
                var logServices = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));

                foreach (var urlLog in logServices)
                {
                    _restAPI.SendChatBot(new RestAPIRequest()
                    {
                        Body = JsonConvert.SerializeObject(requestUpdateContactChatBot),
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
                        Url = $"{urlLog}/api/SaveDataChatBot/UpdateContactChatSub/{tenantId}"
                    });
                }

                UpdateContactChatSub(requestUpdateContactChatBot, tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateContactChat: {ex.Message}");
            }
        }

        public void UpdateContactChatSub(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId)
        {
            try
            {
                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(tenantId).Context;
                CB_Customer? customer = tenantContext.Context.CB_Customers.FirstOrDefault(ptr => ptr.SenderId == requestUpdateContactChatBot.SenderId);
                if (customer != null)
                {
                    customer.ContactId = requestUpdateContactChatBot.ContactId;
                    tenantContext.Context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateContactChatSub: {ex.Message}");
            }
        }

        public void InputICIsSupport(RequestInsertDataChatBot? requestInsertDataChatBot)
        {
            try
            {
                if (requestInsertDataChatBot != null)
                {
                    var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == requestInsertDataChatBot.tenantId);

                    if (configChat != null)
                    {
                        var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                        var tenantContext = tContext.GetTenantContext(requestInsertDataChatBot.tenantId).Context;
                        this._executeWF.SetTenantContext(tenantContext);

                        var result = _executeWF.Execute(configChat.WF_SendChatIC, requestInsertDataChatBot.bodyData, requestInsertDataChatBot.tenantId);

                        tenantContext.Context.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"InputICCheck SessionSupport: {ex.Message}");
            }
        }

        public ChatSessionDaily? GetChatSessionDaily(string tenantId, string senderId)
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        string basePath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseChatBot, tenantId);
                        string filePath = _commonData.GetFilePath(basePath, DateTime.Now.AddDays(-i));
                        string PathdbDayDb = Path.Combine(filePath, $"chatbot.db");
                        if (!File.Exists(PathdbDayDb))
                        {
                            continue;
                        }

                        var sessionObj = SessionChatBot.GetSession(senderId);

                        using (var dbContextDaily = _contextFactory.CreateContext(PathdbDayDb))
                        {
                            ChatSessionDaily? chatSession;
                            if (sessionObj != null && !string.IsNullOrEmpty(sessionObj.SessionId))
                            {
                                chatSession = dbContextDaily.Context.ChatSessionDailies.AsNoTracking().FirstOrDefault(ptr1 => ptr1.SenderId == senderId && ptr1.IsClose == false && ptr1.SessionId == sessionObj.SessionId);
                            }
                            else
                            {
                                chatSession = dbContextDaily.Context.ChatSessionDailies.AsNoTracking().FirstOrDefault(ptr1 => ptr1.SenderId == senderId && ptr1.IsClose == false);
                            }

                            if (chatSession == null)
                            {
                                continue;
                            }
                            else
                            {
                                return chatSession;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"for i = {i} GetChatSessionDaily: {ex.Message} \n senderId: {senderId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GetChatSessionDaily: {ex.Message} \n senderId: {senderId}");
            }

            return null;
        }

        public ResponseCreateCRM IdentificationCRM(CB_ConfigChat configChat, InfoCustomer infoCustomer)
        {
            ResponseCreateCRM responseCreateCRM = new ResponseCreateCRM();
            try
            {
                var tContext = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext = tContext.GetTenantContext(configChat.tenant_id).Context;
                this._executeWF.SetTenantContext(tenantContext);

                try
                {
                    var result = this._executeWF.Execute(configChat.WF_CustomerIdentification, JsonConvert.SerializeObject(infoCustomer), configChat.tenant_id);
                    responseCreateCRM = JsonConvert.DeserializeObject<Models.CreateFile.CRM.Respone.ResponseCreateCRM>(JsonConvert.SerializeObject(result));
                }
                catch (Exception ex)
                {
                    Log.Error($"IdentificationCustomer: {ex.Message} \n Execute:{configChat.WF_CustomerIdentification} \n infoCustomer: {JsonConvert.SerializeObject(infoCustomer)}");
                }

                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
            }

            return responseCreateCRM;
        }

        public void InsertOrUpdateCustomerChat(InfoCustomer infoCustomer, string tenantId)
        {
            try
            {
                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(tenantId).Context;
                tenantContext.Context.Database.ExecuteSqlRawAsync("EXEC InsertOrUpdateCustomers @sender_id, @customerInfo, @cif_list_data, @phoneNo, @contactId",
                                         new SqlParameter("@sender_id", infoCustomer.sender_id),
                                         new SqlParameter("@customerInfo", infoCustomer.customerInfo ?? ""),
                                         new SqlParameter("@cif_list_data", infoCustomer.cif_list_data ?? ""),
                                         new SqlParameter("@phoneNo", infoCustomer.phoneNo ?? ""),
                                         new SqlParameter("@contactId", infoCustomer.contactId ?? ""));
                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Insert or Update Customer: {ex.Message}");
            }
        }
    }
}