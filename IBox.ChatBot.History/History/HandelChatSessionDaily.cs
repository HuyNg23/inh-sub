using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using IBox.ChatBot.DB.DBChatDay;
using IBox.ChatBot.History.Model;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.ConsistentHashing;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace IBox.ChatBot.History.History
{
    public class HandelChatSessionDaily : IHandelChatSessionDaily
    {
        public readonly ICommonData _commonData;
        public readonly IConfiguration _configuration;
        public readonly IRestAPI _restAPI;
        public readonly IEncryption _encryption;
        public readonly IChangeDBChatDay _changeDBChatDay;
        private readonly DBChatDayContextFactory _contextFactory;
        private readonly IBContext<TenantContext> _tenantContext;

        public HandelChatSessionDaily(ICommonData commonData, DBChatDayContextFactory contextFactory, IConfiguration configuration, IRestAPI restAPI, IBContext<TenantContext> tenantContext, IEncryption encryption, IChangeDBChatDay changeDBChatDay)
        {
            _commonData = commonData;
            _contextFactory = contextFactory;
            _configuration = configuration;
            _restAPI = restAPI;
            _tenantContext = tenantContext;
            _encryption = encryption;
            _changeDBChatDay = changeDBChatDay;
        }

        public ResponseGetChatSessionDaily GetChatSessionDaily(RequestGetChatSessionDaily requestGetChatSessionDaily, string Authorize)
        {
            try
            {
                ResponseGetChatSessionDaily responseGetChatSessionDaily = new ResponseGetChatSessionDaily();
                responseGetChatSessionDaily.Data = new List<GetChatSessionDaily>();
                var LogService = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                foreach (var urlLogService in LogService)
                {
                    try
                    {
                        var result = _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(requestGetChatSessionDaily),
                            Headers = new List<RestAPIHeader>() {
                                new RestAPIHeader(){
                                    Label = "Content-Type",
                                    Value = "application/json"
                                },
                                 new RestAPIHeader()
                                {
                                    Label = "Authorization",
                                    Value = Authorize
                                }
                            },
                            Method = "POST",
                            Timeout = 30,
                            Url = $"{urlLogService}/api/ShowData/ShowHistorySessionChat"
                        });


                        var chatMessage = JsonConvert.DeserializeObject<ResponseGetChatSessionDaily>(result.Result);

                        if (chatMessage == null || chatMessage.Data == null)
                        {
                            continue;
                        }

                        responseGetChatSessionDaily.TotalReCords = responseGetChatSessionDaily.TotalReCords + chatMessage.TotalReCords;
                        responseGetChatSessionDaily.Data.AddRange(chatMessage.Data);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"urlLogService GetChatSessionDaily: {ex.Message}");
                    }
                }
                var dataChatSession = GetDataChatSession(requestGetChatSessionDaily);

                if (dataChatSession != null && dataChatSession.Data != null && dataChatSession.Data.Count > 0)
                {
                    responseGetChatSessionDaily.Data.AddRange(dataChatSession.Data);
                    responseGetChatSessionDaily.TotalReCords = responseGetChatSessionDaily.TotalReCords + dataChatSession.TotalReCords;
                }

                int skip = (requestGetChatSessionDaily.PageNum - 1) * 30;
                int take = 30;

                responseGetChatSessionDaily.Data = responseGetChatSessionDaily.Data
                    .OrderByDescending(ptr => ptr.DateMessageLast)
                    .Skip(skip)
                    .Take(take).ToList();
                return responseGetChatSessionDaily;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ResponseGetChatSessionDaily GetDataChatSession(RequestGetChatSessionDaily requestGetChatSessionDaily)
        {
            try
            {
                int pageSize = 30;
                int skipCount = (requestGetChatSessionDaily.PageNum - 1) * pageSize;

                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == requestGetChatSessionDaily.TenantId);
                if (configChat == null)
                {
                    Log.Error($"GetDataChatSession: configChat is null tenant {requestGetChatSessionDaily.TenantId}");
                    return new ResponseGetChatSessionDaily();
                }

                DateTime dateTimeFrom = _commonData.ToDate1(requestGetChatSessionDaily.CreatedDateFrom);

                string pathcurrent = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseChatBot, requestGetChatSessionDaily.TenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, dateTimeFrom);
                List<string> fileDB = _changeDBChatDay.ConvertPathDB(filePathcurrent, dateTimeFrom, configChat);
                List<GetChatSessionDaily> getChatSessionDailies = GetChatSessionsPagedParallel(fileDB, requestGetChatSessionDaily);
                int totalCount = CountTotalRecordsParallel(fileDB, requestGetChatSessionDaily);

                return new ResponseGetChatSessionDaily()
                {
                    Data = getChatSessionDailies,
                    TotalReCords = totalCount,
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetDataChatSession: {ex.Message}");
                return new ResponseGetChatSessionDaily();
            }
        }
        public List<GetChatSessionDaily> GetChatSessionsPagedParallel(List<string> dbFiles, RequestGetChatSessionDaily request)
        {
            int skip = (request.PageNum - 1) * 30;
            int take = 30;
            int totalNeeded = skip + take;

            var collected = new ConcurrentBag<GetChatSessionDaily>();
            int currentTotal = 0;

            var dateTimeFrom = _commonData.ToDate1(request.CreatedDateFrom);
            var dateTimeTo = _commonData.ToDate1(request.CreatedDateTo);
            bool? isClose = request.IsClose == 1 ? true : request.IsClose == 0 ? false : null;
            bool? isSupport = request.IsSupport == 1 ? true : request.IsSupport == 0 ? false : null;

            Parallel.ForEach(dbFiles, (dbPath, state) =>
            {
                if (Volatile.Read(ref currentTotal) >= totalNeeded * 2)
                {
                    state.Stop();
                    return;
                }

                using var context = _contextFactory.CreateContext(dbPath);

                var query = context.Context.ChatSessionDailies.AsNoTracking();
                query = ApplyChatSessionFilters(query, request, dateTimeFrom, dateTimeTo, isClose, isSupport);

                var sessions = query
                    .OrderByDescending(x => x.DateMessageLast)
                    .Take(totalNeeded)
                    .Select(session => new GetChatSessionDaily
                    {
                        CustomerName = session.CustomerName,
                        SessionId = session.SessionId,
                        SenderId = session.SenderId,
                        IsClose = session.IsClose,
                        InteractionCRM = session.InteractionCRM,
                        Channel = session.Channel,
                        SubChannel = session.SubChannel,
                        IsSupport = session.IsSupport,
                        DateMessageLast = session.DateMessageLast,
                        Site = IBGlobalConfig.ThisSite,
                        CustomerId = session.CustomerId,
                        IdIc = session.IdIc,
                        TenantId = request.TenantId,
                        IsSiteClose = session.IsSiteClose
                    })
                    .ToList();

                foreach (var session in sessions)
                {
                    if (Volatile.Read(ref currentTotal) >= totalNeeded * 2)
                        break;

                    collected.Add(session);
                    Interlocked.Increment(ref currentTotal);
                }
            });


            return collected
                .OrderByDescending(x => x.DateMessageLast)
                .Skip(skip)
                .Take(take)
                .ToList();
        }
        public static IQueryable<ChatSessionDaily> ApplyChatSessionFilters(IQueryable<ChatSessionDaily> query, RequestGetChatSessionDaily request, DateTime dateTimeFrom, DateTime dateTimeTo, bool? isClose, bool? isSupport)
        {
            if (!string.IsNullOrEmpty(request.SessionId))
                query = query.Where(x => x.SessionId.Contains(request.SessionId));

            if (!string.IsNullOrEmpty(request.SenderId))
                query = query.Where(x => x.SenderId.Contains(request.SenderId));

            if (!string.IsNullOrEmpty(request.InteractionCRM))
                query = query.Where(x => x.InteractionCRM != null && x.InteractionCRM.Contains(request.InteractionCRM));

            if (!string.IsNullOrEmpty(request.CustomerName))
                query = query.Where(x => x.CustomerName.Contains(request.CustomerName));

            if (!string.IsNullOrEmpty(request.InteractionIC))
                query = query.Where(x => x.IdIc != null && x.IdIc.Contains(request.InteractionIC));

            if (!string.IsNullOrEmpty(request.CustomerIdIC))
                query = query.Where(x => x.CustomerId != null && x.CustomerId.Contains(request.CustomerIdIC));

            if (!string.IsNullOrEmpty(request.Channel))
                query = query.Where(x => x.Channel != null && x.Channel.Contains(request.Channel));

            if (!string.IsNullOrEmpty(request.SubChannel))
                query = query.Where(x => x.SubChannel != null && x.SubChannel.Contains(request.SubChannel));

            if (request.IsClose.HasValue)
                query = query.Where(x => x.IsClose == isClose);

            if (request.IsSupport.HasValue)
                query = query.Where(x => x.IsSupport == isSupport);

            query = query.Where(x => x.DateMessageLast >= dateTimeFrom && x.DateMessageLast <= dateTimeTo);

            return query;
        }

        public int CountTotalRecordsParallel(List<string> dbFiles, RequestGetChatSessionDaily requestGetChatSessionDaily)
        {
            object lockObj = new();
            int totalCount = 0;

            Parallel.ForEach(dbFiles, dbFile =>
            {
                var count = CountFromDb(dbFile, requestGetChatSessionDaily);
                lock (lockObj)
                {
                    totalCount += count;
                }
            });

            return totalCount;
        }

        private int CountFromDb(string dbPath, RequestGetChatSessionDaily requestGetChatSessionDaily)
        {
            int PageNum = 15;
            int CurrentRow = (requestGetChatSessionDaily.PageNum - 1) * PageNum;
            DateTime dateTimeFrom = _commonData.ToDate1(requestGetChatSessionDaily.CreatedDateFrom);
            DateTime dateTimeTo = _commonData.ToDate1(requestGetChatSessionDaily.CreatedDateTo);
            bool? isClose;
            if (requestGetChatSessionDaily.IsClose == 1)
            {
                isClose = true;
            }
            else if (requestGetChatSessionDaily.IsClose == 0)
            {
                isClose = false;
            }
            else
            {
                isClose = null;
            }

            bool? isSupport;
            if (requestGetChatSessionDaily.IsSupport == 1)
            {
                isSupport = true;
            }
            else if (requestGetChatSessionDaily.IsSupport == 0)
            {
                isSupport = false;
            }
            else
            {
                isSupport = null;
            }

            using (var dbContextDaily = _contextFactory.CreateContext(dbPath))
            {
                return dbContextDaily.Context.ChatSessionDailies.AsNoTracking()
                   .Where(session =>
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.SessionId) || session.SessionId.Contains(requestGetChatSessionDaily.SessionId)) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.SenderId) || session.SenderId.Contains(requestGetChatSessionDaily.SenderId)) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionCRM) || (session.InteractionCRM != null && session.InteractionCRM.Contains(requestGetChatSessionDaily.InteractionCRM))) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerName) || session.CustomerName.Contains(requestGetChatSessionDaily.CustomerName)) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionIC) || (session.IdIc != null && session.IdIc.Contains(requestGetChatSessionDaily.InteractionIC))) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerIdIC) || (session.CustomerId != null && session.CustomerId.Contains(requestGetChatSessionDaily.CustomerIdIC))) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.Channel) || (session.Channel != null && session.Channel.Contains(requestGetChatSessionDaily.Channel))) &&
                       (string.IsNullOrEmpty(requestGetChatSessionDaily.SubChannel) || (session.SubChannel != null && session.SubChannel.Contains(requestGetChatSessionDaily.SubChannel))) &&
                        (!requestGetChatSessionDaily.IsClose.HasValue || session.IsClose == isClose) &&
                       (!requestGetChatSessionDaily.IsSupport.HasValue || session.IsSupport == isSupport) &&
                       session.DateMessageLast >= dateTimeFrom &&
                       session.DateMessageLast <= dateTimeTo
                   )
                   .Count();
            }
        }

        public ResponseGetChatSessionDaily SelectDBSession(RequestGetChatSessionDaily requestGetChatSessionDaily, string pathDBTemporary)
        {
            try
            {
                int PageNum = 15;
                int CurrentRow = (requestGetChatSessionDaily.PageNum - 1) * PageNum;
                DateTime dateTimeFrom = _commonData.ToDate1(requestGetChatSessionDaily.CreatedDateFrom);
                DateTime dateTimeTo = _commonData.ToDate1(requestGetChatSessionDaily.CreatedDateTo);
                bool? isClose;
                if (requestGetChatSessionDaily.IsClose == 1)
                {
                    isClose = true;
                }
                else if (requestGetChatSessionDaily.IsClose == 0)
                {
                    isClose = false;
                }
                else
                {
                    isClose = null;
                }

                bool? isSupport;
                if (requestGetChatSessionDaily.IsSupport == 1)
                {
                    isSupport = true;
                }
                else if (requestGetChatSessionDaily.IsSupport == 0)
                {
                    isSupport = false;
                }
                else
                {
                    isSupport = null;
                }

                List<GetChatSessionDaily>? query;
                int totalCount = 0;
                using (var dbContextDaily = _contextFactory.CreateContext(pathDBTemporary))
                {
                    query = (from ChatSessionDaily in dbContextDaily.Context.ChatSessionDailies.AsNoTracking()
                             where (string.IsNullOrEmpty(requestGetChatSessionDaily.SessionId) || ChatSessionDaily.SessionId.Contains(requestGetChatSessionDaily.SessionId))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.SenderId) || ChatSessionDaily.SenderId.Contains(requestGetChatSessionDaily.SenderId))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionCRM) || ChatSessionDaily.InteractionCRM != null && ChatSessionDaily.InteractionCRM.Contains(requestGetChatSessionDaily.InteractionCRM))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerName) || ChatSessionDaily.CustomerName.Contains(requestGetChatSessionDaily.CustomerName))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionIC) || ChatSessionDaily.IdIc != null && ChatSessionDaily.IdIc.Contains(requestGetChatSessionDaily.InteractionIC))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerIdIC) || ChatSessionDaily.CustomerId != null && ChatSessionDaily.CustomerId.Contains(requestGetChatSessionDaily.CustomerIdIC))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.Channel) || ChatSessionDaily.Channel != null && ChatSessionDaily.Channel.Contains(requestGetChatSessionDaily.Channel))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.SubChannel) || ChatSessionDaily.SubChannel != null && ChatSessionDaily.SubChannel.Contains(requestGetChatSessionDaily.SubChannel))
                                 && (!requestGetChatSessionDaily.IsClose.HasValue || ChatSessionDaily.IsClose == isClose)
                                 && (!requestGetChatSessionDaily.IsSupport.HasValue || ChatSessionDaily.IsSupport == isSupport)
                                 && dateTimeFrom <= ChatSessionDaily.DateMessageLast && ChatSessionDaily.DateMessageLast <= dateTimeTo
                             select new GetChatSessionDaily()
                             {
                                 CustomerName = ChatSessionDaily.CustomerName,
                                 SessionId = ChatSessionDaily.SessionId,
                                 SenderId = ChatSessionDaily.SenderId,
                                 IsClose = ChatSessionDaily.IsClose,
                                 InteractionCRM = ChatSessionDaily.InteractionCRM,
                                 Channel = ChatSessionDaily.Channel,
                                 SubChannel = ChatSessionDaily.SubChannel,
                                 IsSupport = ChatSessionDaily.IsSupport,
                                 DateMessageLast = ChatSessionDaily.DateMessageLast,
                                 Site = IBGlobalConfig.ThisSite,
                                 CustomerId = ChatSessionDaily.CustomerId,
                                 IdIc = ChatSessionDaily.IdIc,
                                 TenantId = requestGetChatSessionDaily.TenantId,
                                 IsSiteClose = ChatSessionDaily.IsSiteClose
                             }).Skip(CurrentRow).Take(PageNum).ToList();
                    totalCount = (from ChatSessionDaily in dbContextDaily.Context.ChatSessionDailies.AsNoTracking()
                                  where (string.IsNullOrEmpty(requestGetChatSessionDaily.SessionId) || ChatSessionDaily.SessionId.Contains(requestGetChatSessionDaily.SessionId))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.SenderId) || ChatSessionDaily.SenderId.Contains(requestGetChatSessionDaily.SenderId))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionCRM) || ChatSessionDaily.InteractionCRM != null && ChatSessionDaily.InteractionCRM.Contains(requestGetChatSessionDaily.InteractionCRM))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerName) || ChatSessionDaily.CustomerName.Contains(requestGetChatSessionDaily.CustomerName))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.InteractionIC) || ChatSessionDaily.IdIc != null && ChatSessionDaily.IdIc.Contains(requestGetChatSessionDaily.InteractionIC))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.CustomerIdIC) || ChatSessionDaily.CustomerId != null && ChatSessionDaily.CustomerId.Contains(requestGetChatSessionDaily.CustomerIdIC))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.Channel) || ChatSessionDaily.Channel != null && ChatSessionDaily.Channel.Contains(requestGetChatSessionDaily.Channel))
                                 && (string.IsNullOrEmpty(requestGetChatSessionDaily.SubChannel) || ChatSessionDaily.SubChannel != null && ChatSessionDaily.SubChannel.Contains(requestGetChatSessionDaily.SubChannel))
                                 && (!requestGetChatSessionDaily.IsClose.HasValue || ChatSessionDaily.IsClose == isClose)
                                 && (!requestGetChatSessionDaily.IsSupport.HasValue || ChatSessionDaily.IsSupport == isSupport)
                                 && dateTimeFrom <= ChatSessionDaily.DateMessageLast && ChatSessionDaily.DateMessageLast <= dateTimeTo
                                  select ChatSessionDaily).Count();
                }
                return new ResponseGetChatSessionDaily()
                {
                    Data = query.OrderByDescending(ptr => ptr.DateMessageLast).ToList(),
                    TotalReCords = totalCount,
                };
            }
            catch (Exception ex)
            {
                Log.Error($"SelectDBSession: {ex.Message}");
                return new ResponseGetChatSessionDaily();
            }
        }

        public ResponseGetChatMessageDaily GetChatMessageDaily(RequestGetChatMessageDaily requestGetChatMessageDaily, string Authorize)
        {
            try
            {
                ResponseGetChatMessageDaily responseGetChatMessageDaily = new ResponseGetChatMessageDaily();
                responseGetChatMessageDaily.Data = new List<GetChatMessageDaily>();
                var LogService = IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite));
                foreach (var urlLogService in LogService)
                {
                    try
                    {
                        var result = _restAPI.SendChatBot(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(requestGetChatMessageDaily),
                            Headers = new List<RestAPIHeader>() {
                                new RestAPIHeader(){
                                    Label = "Content-Type",
                                    Value = "application/json"
                                },
                                new RestAPIHeader()
                                {
                                    Label = "Authorization",
                                    Value = Authorize
                                }
                            },
                            Method = "POST",
                            Timeout = 30,
                            Url = $"{urlLogService}/api/ShowData/ShowHistoryMessageChat"
                        });


                        var chatMessage = JsonConvert.DeserializeObject<ResponseGetChatMessageDaily>(result.Result);

                        if (chatMessage == null || chatMessage.Data == null)
                        {
                            continue;
                        }

                        responseGetChatMessageDaily.Data.AddRange(chatMessage.Data);
                        responseGetChatMessageDaily.TotalReCords = responseGetChatMessageDaily.TotalReCords + chatMessage.TotalReCords;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call api GetChatMessageDaily urlLogService: {ex.Message}");
                    }
                }
                var dataChatMessage = GetDataChatMessage(requestGetChatMessageDaily);
                responseGetChatMessageDaily.TotalReCords = responseGetChatMessageDaily.TotalReCords + dataChatMessage.TotalReCords;
                if (dataChatMessage != null && dataChatMessage.Data != null && dataChatMessage.Data.Count > 0)
                {
                    responseGetChatMessageDaily.Data.AddRange(dataChatMessage.Data);
                }

                responseGetChatMessageDaily.Data = responseGetChatMessageDaily.Data.OrderByDescending(ptr => ptr.CreatedDate).ToList();
                return responseGetChatMessageDaily;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ResponseGetChatMessageDaily GetDataChatMessage(RequestGetChatMessageDaily requestGetChatMessageDaily)
        {
            try
            {
                if (requestGetChatMessageDaily == null)
                {
                    Log.Error($"GetDataChatSession: requestGetChatSessionDaily is null");
                    return new ResponseGetChatMessageDaily();
                }

                var configChat = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == requestGetChatMessageDaily.TenantId);
                if (configChat == null)
                {
                    Log.Error($"GetDataChatSession: configChat is null tenant {requestGetChatMessageDaily.TenantId} this site {IBGlobalConfig.ThisSite}");
                    return new ResponseGetChatMessageDaily();
                }

                DateTime getDateTime = _commonData.ToDate1(requestGetChatMessageDaily.DateTimeCurrent);
                string pathcurrent = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseChatBot, requestGetChatMessageDaily.TenantId);
                string filePathcurrent = _commonData.GetFilePath(pathcurrent, getDateTime);
                string dbName = ConsistentHashing.GetChatBotDB(requestGetChatMessageDaily.SenderId);
                string fileName = Path.GetFileNameWithoutExtension(dbName);
                var fileDB = Directory.Exists(filePathcurrent) ? Directory.GetFiles(filePathcurrent, $"{fileName}*.db").OrderByDescending(f => File.GetLastWriteTime(f)).ToList() : new List<string>();
                if (fileDB.Count == 0)
                {
                    string pathDBZip = Path.Combine(Directory.GetCurrentDirectory(), configChat.PathBackUp, configChat.tenant_id);
                    string dateInput = getDateTime.ToString("yyyyMMdd");
                    string fileName1 = $"BackupChatBot_{dateInput}.zip";
                    string zipFolder = Path.Combine(Directory.GetCurrentDirectory(), configChat.PathBackUp, configChat.tenant_id);
                    string zipFilePath = Path.Combine(zipFolder, fileName1);

                    string pattern = $@"^{fileName}(_\d+)?\.db$";
                    string extractPath = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderZip, requestGetChatMessageDaily.TenantId);

                    _commonData.ExtractFileZipPattern(zipFilePath, extractPath, pattern);
                    string filePathcurrent1 = _commonData.GetFilePath(extractPath, getDateTime);
                    var fileDB1 = Directory.Exists(filePathcurrent1) ? Directory.GetFiles(filePathcurrent1, $"{fileName}*.db").OrderByDescending(f => File.GetLastWriteTime(f)).ToList() : new List<string>();

                    var data1 = GetChatMessagePagedParallel(fileDB1, requestGetChatMessageDaily);
                    var countData1 = CountTotalRecordsMessageParallel(fileDB1, requestGetChatMessageDaily);

                    return new ResponseGetChatMessageDaily()
                    {
                        Data = data1,
                        TotalReCords = countData1
                    };
                }

                var data = GetChatMessagePagedParallel(fileDB, requestGetChatMessageDaily);
                var countData = CountTotalRecordsMessageParallel(fileDB, requestGetChatMessageDaily);

                return new ResponseGetChatMessageDaily()
                {
                    Data = data,
                    TotalReCords = countData
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetDataChatSession: {ex.Message}");
                return new ResponseGetChatMessageDaily();
            }
        }

        public List<GetChatMessageDaily> GetChatMessagePagedParallel(List<string> dbFiles, RequestGetChatMessageDaily requestGetChatMessageDaily)
        {
            int skip = (requestGetChatMessageDaily.PageNum - 1) * 30;
            int take = 30;
            int totalNeeded = skip + take;

            var collected = new ConcurrentBag<GetChatMessageDaily>();
            int currentTotal = 0;
            Parallel.ForEach(dbFiles, (dbPath, state) =>
            {
                if (Volatile.Read(ref currentTotal) >= totalNeeded * 2)
                {
                    state.Stop();
                    return;
                }

                using var context = _contextFactory.CreateContext(dbPath);
                var messages = context.Context.ChatMessageDailies.AsNoTracking()
                    .Where(ptr => ptr.SessionId == requestGetChatMessageDaily.SessionId)
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(session => new GetChatMessageDaily
                    {
                        Id = session.Id,
                        SessionId = session.SessionId,
                        MessageContent = session.MessageContent,
                        IsInputIC = session.IsInputIC,
                        Site = IBGlobalConfig.ThisSite,
                        CreatedDate = session.CreatedDate
                    })
                    .Take(totalNeeded)
                    .ToList();

                foreach (var message in messages)
                {
                    if (Volatile.Read(ref currentTotal) >= totalNeeded * 2)
                    {
                        break;
                    }

                    collected.Add(message);
                    Interlocked.Increment(ref currentTotal);
                }
            });

            return collected
                .OrderByDescending(x => x.CreatedDate)
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        public int CountTotalRecordsMessageParallel(List<string> dbFiles, RequestGetChatMessageDaily requestGetChatMessageDaily)
        {
            object lockObj = new();
            int totalCount = 0;

            Parallel.ForEach(dbFiles, dbFile =>
            {
                var count = CountFromMessageDb(dbFile, requestGetChatMessageDaily);
                lock (lockObj)
                {
                    totalCount += count;
                }
            });

            return totalCount;
        }

        private int CountFromMessageDb(string dbPath, RequestGetChatMessageDaily requestGetChatMessageDaily)
        {
            using (var dbContextDaily = _contextFactory.CreateContext(dbPath))
            {
                return dbContextDaily.Context.ChatMessageDailies.AsNoTracking()
                .Where(ptr => ptr.SessionId == requestGetChatMessageDaily.SessionId)
                .OrderByDescending(x => x.CreatedDate)
                .Count();
            }
        }

        public ResponseGetChatMessageDaily SelectDBMessage(RequestGetChatMessageDaily requestGetChatMessageDaily, string pathDBTemporary)
        {
            try
            {
                int PageNum = 15;
                int CurrentRow = (requestGetChatMessageDaily.PageNum - 1) * PageNum;
                List<GetChatMessageDaily> query;
                int totalCount = 0;
                using (var dbContextDaily = _contextFactory.CreateContext(pathDBTemporary))
                {
                    query = (from ChatMessageDaily in dbContextDaily.Context.ChatMessageDailies.AsNoTracking()
                             where ChatMessageDaily.SessionId == requestGetChatMessageDaily.SessionId
                             orderby ChatMessageDaily.CreatedDate descending
                             select new GetChatMessageDaily()
                             {
                                 Id = ChatMessageDaily.Id,
                                 SessionId = ChatMessageDaily.SessionId,
                                 MessageContent = ChatMessageDaily.MessageContent,
                                 IsInputIC = ChatMessageDaily.IsInputIC,
                                 Site = IBGlobalConfig.ThisSite,
                                 CreatedDate = ChatMessageDaily.CreatedDate
                             }).Skip(CurrentRow).Take(PageNum).ToList();
                    totalCount = (from ChatMessageDaily in dbContextDaily.Context.ChatMessageDailies.AsNoTracking()
                                  where ChatMessageDaily.SessionId == requestGetChatMessageDaily.SessionId
                                  select ChatMessageDaily).Count();
                }
                return new ResponseGetChatMessageDaily()
                {
                    Data = query,
                    TotalReCords = totalCount,
                };
            }
            catch (Exception ex)
            {
                Log.Error($"SelectDBSession: {ex.Message}");
                return new ResponseGetChatMessageDaily();
            }
        }

        public ResponseHistoryZipFile ShowHistoryZipFile(RequestHistoryZipFile requestHistoryZipFile)
        {
            try
            {
                DateTime startDate;
                DateTime endDate;

                if (string.IsNullOrEmpty(requestHistoryZipFile.StartDate) || string.IsNullOrEmpty(requestHistoryZipFile.EndDate))
                {
                    startDate = DateTime.Now;
                    endDate = DateTime.Now;
                    startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
                    endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, DateTimeKind.Utc);
                }
                else
                {
                    startDate = DateTime.ParseExact(requestHistoryZipFile.StartDate, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    endDate = DateTime.ParseExact(requestHistoryZipFile.EndDate, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                }

                int PageNum = 30;
                int CurrentRow = (requestHistoryZipFile.PageNum - 1) * PageNum;


                var query = (from g in _tenantContext.Context.CB_ZipFileHistories
                             where g.CreatedDate >= startDate && g.CreatedDate <= endDate &&
                            (string.IsNullOrEmpty(requestHistoryZipFile.NameFile) || g.NameFile.Contains(requestHistoryZipFile.NameFile)
                            && (string.IsNullOrEmpty(requestHistoryZipFile.ThisSite) || g.ThisSite.Contains(requestHistoryZipFile.ThisSite)))
                             orderby g.CreatedDate descending
                             select new HistoryZipFile()
                             {
                                 Id = g.Id,
                                 CreatedDate = g.CreatedDate,
                                 ListFileZip = g.ListFileZip,
                                 NameFile = g.NameFile,
                                 PathFile = g.PathFile,
                                 ThisSite = g.ThisSite,
                             }).Skip(CurrentRow).Take(PageNum);




                int totalRecords = (from g in _tenantContext.Context.CB_ZipFileHistories
                                    where g.CreatedDate >= startDate && g.CreatedDate <= endDate &&
                                   (string.IsNullOrEmpty(requestHistoryZipFile.NameFile) || g.NameFile.Contains(requestHistoryZipFile.NameFile)
                                   && (string.IsNullOrEmpty(requestHistoryZipFile.ThisSite) || g.ThisSite.Contains(requestHistoryZipFile.ThisSite)))
                                    select g).Count();



                return new ResponseHistoryZipFile()
                {
                    Data = query.ToList(),
                    TotalReCords = totalRecords
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ResponseSizeFolder ShowSizeDriveFolder(TypeUserBase typeUserBase)
        {
            try
            {
                switch (typeUserBase)
                {
                    case TypeUserBase.Root:
                        return ShowSizeDriveFolderTenant();

                    case TypeUserBase.Tenant:
                        return ShowSizeDriveFolderTenant();

                    default:
                        throw new IboxLog($"Not found typeUserBase: {typeUserBase}", "AppLogs");
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An error occurred while showSizeDriveFolder: {ex.Message}", "AppLogs");
            }
        }

        public ResponseSizeFolder ShowSizeDriveFolderRoot()
        {
            return new ResponseSizeFolder();
        }

        public ResponseSizeFolder ShowSizeDriveFolderTenant()
        {
            string thisSite = IBGlobalConfig.ThisSite ?? string.Empty;
            string folderPath = Directory.GetCurrentDirectory();
            Log.Information($"folderPath: {folderPath}");
            var response = new ResponseSizeFolder();

            folderPath = $"{folderPath}/{DataPath.DataBaseChatBot}";

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                //DriveInfo drive = new DriveInfo($"C:");

                //if (drive.IsReady)
                //{
                //    responseShowSizeDriverFolder.CapacityDriver = drive.TotalSize / (1024 * 1024);
                //}

                //long folderSize = GetDirectorySize(folderPath);
                //responseShowSizeDriverFolder.CapacityFolder = folderSize / (1024 * 1024);
                //return response;

                return response;
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {

                string currentDrive = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);

                DriveInfo[] driversList = DriveInfo.GetDrives();

                DriveInfo currentDriveInfo = new DriveInfo(currentDrive);


                long totalSize = 0;

                foreach (DriveInfo d in driversList)
                {
                    if (d.DriveType == DriveType.Fixed && d.Name == currentDriveInfo.Name && d.IsReady)
                    {
                        totalSize = d.TotalSize;

                        Log.Information("Drive {0}", d.Name);
                        Log.Information("  File type: {0}", d.DriveType);

                        break;
                    }
                }

                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(folderPath);
                    DirectoryInfo[] dirs = directoryInfo.GetDirectories();

                    foreach (DirectoryInfo dir in dirs)
                    {
                        long folderSize = GetDirectorySize(dir.FullName);

                        var sizeFolderTenant = new List<ResponseSizeFolderTenant>();

                        sizeFolderTenant.Add(new ResponseSizeFolderTenant()
                        {
                            TenantId = dir.Name,
                            TotalSizeFolder = folderSize
                        });

                        response.SizeTenant = sizeFolderTenant;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred while retrieving directory information: {ex.Message}");
                }

                response.TotalSizeDisk = totalSize;
                response.ThisSite = thisSite;

                return response;
            }

            return response;
        }

        public ResponseSizeFolder ShowSizeDriveFolderOnRing(string tenantId)
        {
            var response = new ResponseSizeFolder();

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    response = GetFolderSizeForPlatform(tenantId, "\\");
                    break;

                case PlatformID.Unix:
                    response = GetFolderSizeForPlatform(tenantId, "/");
                    break;

                default:
                    Log.Error("Unsupported platform.");
                    break;
            }

            response.ThisSite = IBGlobalConfig.ThisSite ?? string.Empty;

            return response;
        }

        private ResponseSizeFolder GetFolderSizeForPlatform(string tenantId, string separator)
        {
            var response = new ResponseSizeFolder();

            string folderPath = GetFolderPath(separator);
            if (string.IsNullOrEmpty(folderPath))
            {
                return response;
            }

            long totalSize = GetCurrentDriveSize();
            response.TotalSizeDisk = totalSize;

            response.SizeTenant = GetTenantFolderSize(folderPath, tenantId);
            return response;
        }

        private string GetFolderPath(string separator)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            return Path.Combine(currentDirectory, DataPath.DataBaseChatBot);
        }

        private long GetCurrentDriveSize()
        {
            string currentDomain = AppDomain.CurrentDomain.BaseDirectory;

            if (string.IsNullOrEmpty(currentDomain))
            {
                Log.Error("AppDomain base directory is null or empty.");
                return 0;
            }


            string currentDrive = Path.GetPathRoot(currentDomain);

            if (string.IsNullOrEmpty(currentDrive))
            {
                Log.Error("Failed to determine the root drive.");
                return 0;
            }

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.Name == currentDrive && drive.IsReady)
                {
                    return drive.TotalSize;
                }
            }

            Log.Error("Unable to retrieve total size for the drive.");
            return 0;
        }

        private List<ResponseSizeFolderTenant> GetTenantFolderSize(string folderPath, string tenantId)
        {
            var sizeFolderTenant = new List<ResponseSizeFolderTenant>();

            try
            {
                var directoryInfo = new DirectoryInfo(folderPath);
                if (!directoryInfo.Exists)
                {
                    Log.Error($"Directory does not exist: {folderPath}");
                    return sizeFolderTenant;
                }

                DirectoryInfo[] tenantDirectories = directoryInfo.GetDirectories();
                if (tenantDirectories.Length == 0)
                {
                    Log.Information($"No tenant directories found in '{folderPath}'");
                    sizeFolderTenant.Add(new ResponseSizeFolderTenant { TenantId = "", TotalSizeFolder = 0 });
                    return sizeFolderTenant;
                }

                foreach (DirectoryInfo tenantDir in tenantDirectories)
                {
                    if (tenantDir.Name == tenantId)
                    {
                        long tenantFolderSize = GetDirectorySize(tenantDir.FullName);
                        sizeFolderTenant.Add(new ResponseSizeFolderTenant
                        {
                            TenantId = tenantId,
                            TotalSizeFolder = tenantFolderSize
                        });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while retrieving tenant folder size: {ex.Message}");
            }

            return sizeFolderTenant;
        }

        private static long GetDirectorySize(string folderPath)
        {
            long folderSize = 0;
            try
            {
                var directoryInfo = new DirectoryInfo(folderPath);

                // Get all files size in the directory
                FileInfo[] files = directoryInfo.GetFiles();
                folderSize += files.Sum(file => file.Length);

                // Get all subdirectories and recursively calculate their size
                DirectoryInfo[] subDirs = directoryInfo.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    folderSize += GetDirectorySize(subDir.FullName);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error calculating folder size for '{folderPath}': {ex.Message}");
            }

            return folderSize;
        }

        public List<ResponseSizeFolder> GetAllSizeDriveFolderChatBot(HttpRequest requestContext)
        {
            try
            {
                var servers = IBGlobalConfig.LogServiceIBox.ToList();
                List<ResponseSizeFolder> result = new List<ResponseSizeFolder>();
                var authorization = requestContext.Headers["Authorization"].ToString();

                servers.ForEach(ip =>
                {
                    try
                    {
                        var rest = $"{ip}/api/ShowData/ShowSizeDriveFolderOnRing";
                        using (var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual })
                        using (var httpClient = new HttpClient(handler))
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                            requestMessage.Headers.Add("Authorization", authorization);

                            var httpResponse = httpClient.SendAsync(requestMessage).Result;

                            if (httpResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var responseContent = httpResponse.Content.ReadAsStringAsync().Result;
                                var data = JsonConvert.DeserializeObject<ResponseForm<ResponseSizeFolder>>(responseContent);

                                if (data != null && data.Data != null)
                                {
                                    result.Add(data.Data);
                                }
                                else
                                {
                                    Log.Error("Data ShowSizeDriveFolderOnRing is null.");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, ex.Message);
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"ShowSizeDriveFolderOnRing error: {ex}", "AppLogs");
            }
        }

        public ResponseGetSenderDaily GetSenderDaily(RequestGetSender requestGetSender, string? Authorize)
        {
            try
            {
                //Call api đến các server
                ResponseGetSenderDaily responseGetSender = new ResponseGetSenderDaily();
                responseGetSender.Data = new List<ResponseGetSender>();
                var dataSender = GetSender(requestGetSender);
                if (dataSender != null && dataSender.Data != null && dataSender.Data.Count > 0)
                {
                    responseGetSender.TotalReCords = responseGetSender.TotalReCords + dataSender.TotalReCords;
                    responseGetSender.Data.AddRange(dataSender.Data);
                }

                responseGetSender.Data = responseGetSender.Data.OrderByDescending(ptr => ptr.CreatedDate).ToList();
                return responseGetSender;
            }
            catch (Exception ex)
            {
                Log.Error($"GetSenderDaily: {ex.Message}");
                return new ResponseGetSenderDaily();
            }
        }

        public ResponseGetSenderDaily GetSender(RequestGetSender requestGetSender)
        {
            try
            {
                int PageNum = 30;
                int CurrentRow = (requestGetSender.PageNum - 1) * PageNum;
                List<ResponseGetSender> query;
                int totalCount = 0;

                if (requestGetSender == null || requestGetSender.TenantId == null)
                {
                    return new ResponseGetSenderDaily();
                }

                DateTime? startDate;
                DateTime? endDate;

                DateTime? dateTimeFrom = _commonData.ToDate2(requestGetSender?.CreatedDateFrom ?? "");
                DateTime? dateTimeTo = _commonData.ToDate2(requestGetSender?.CreatedDateTo ?? "");
                startDate = dateTimeFrom;
                endDate = dateTimeTo;

                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

                var tenantContext = context.GetTenantContext(requestGetSender.TenantId).Context;


                query = (from Customers in tenantContext.Context.CB_Customers.AsNoTracking()
                         where (!startDate.HasValue || Customers.CreatedDate >= startDate) &&
                                (!endDate.HasValue || Customers.CreatedDate <= endDate)
                         && (string.IsNullOrEmpty(requestGetSender.CRMContactID) || Customers.ContactId != null && Customers.ContactId == requestGetSender.CRMContactID)
                         && (string.IsNullOrEmpty(requestGetSender.SenderId) || Customers.SenderId == requestGetSender.SenderId)
                         && (string.IsNullOrEmpty(requestGetSender.Phone) || Customers.Phone != null && Customers.Phone == requestGetSender.Phone)
                         && (string.IsNullOrEmpty(requestGetSender.cif_list_data) || Customers.cif_list_data != null && Customers.cif_list_data == requestGetSender.cif_list_data)
                         && (string.IsNullOrEmpty(requestGetSender.CifEncryp) || Customers.CustomerInfo != null && Customers.CustomerInfo == requestGetSender.CifEncryp)
                         select new ResponseGetSender()
                         {
                             SenderId = Customers.SenderId,
                             CustomerInfo = Customers.CustomerInfo,
                             cif_list_data = Customers.cif_list_data,
                             Phone = Customers.Phone,
                             ContactId = Customers.ContactId,
                             CreatedDate = Customers.CreatedDate,
                             ThisSite = IBGlobalConfig.ThisSite
                         }).Skip(CurrentRow).Take(PageNum).ToList();
                totalCount = (from Customers in tenantContext.Context.CB_Customers.AsNoTracking()
                              where (!startDate.HasValue || Customers.CreatedDate >= startDate) &&
                                (!endDate.HasValue || Customers.CreatedDate <= endDate)
                         && (string.IsNullOrEmpty(requestGetSender.CRMContactID) || Customers.ContactId != null && Customers.ContactId == requestGetSender.CRMContactID)
                         && (string.IsNullOrEmpty(requestGetSender.SenderId) || Customers.SenderId == requestGetSender.SenderId)
                         && (string.IsNullOrEmpty(requestGetSender.Phone) || Customers.Phone != null && Customers.Phone == requestGetSender.Phone)
                         && (string.IsNullOrEmpty(requestGetSender.cif_list_data) || Customers.cif_list_data != null && Customers.cif_list_data == requestGetSender.cif_list_data)
                         && (string.IsNullOrEmpty(requestGetSender.CifEncryp) || Customers.CustomerInfo != null && Customers.CustomerInfo == requestGetSender.CifEncryp)
                              select Customers).Count();
                return new ResponseGetSenderDaily()
                {
                    Data = query.OrderByDescending(ptr => ptr.CreatedDate).ToList(),
                    TotalReCords = totalCount,
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetSender: {ex.Message}");
                return new ResponseGetSenderDaily();
            }
        }
    }
}