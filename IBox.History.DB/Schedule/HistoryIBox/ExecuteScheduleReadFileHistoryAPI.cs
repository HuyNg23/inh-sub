using IBox.ChatBot.HandleScheduleSQL;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Model;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Serilog;
using System.Text;

namespace IBox.History.DB.Schedule.HistoryIBox
{
    public class ExecuteScheduleReadFileHistoryAPI : IJob
    {
        private readonly DBHistoryContextFactory _historyContextFactory;
        private readonly ICreateDB _createDB;
        private readonly ICommonData _commonData;
        private readonly ICreateDB _iCreateDB;
        private static readonly SemaphoreSlim semaphoreCustomer = new SemaphoreSlim(2);
        private const int batchSize = 500;

        public ExecuteScheduleReadFileHistoryAPI(DBHistoryContextFactory historyContextFactory, ICreateDB createDB, ICommonData commonData, ICreateDB iCreateDB)
        {
            _historyContextFactory = historyContextFactory;
            _createDB = createDB;
            _commonData = commonData;
            _iCreateDB = iCreateDB;
        }

        public Task Execute(IJobExecutionContext context)
        {
            bool acquired = semaphoreCustomer.Wait(0);

            if (!acquired)
            {
                return Task.CompletedTask;
            }

            try
            {
                try
                {
                    ReadFileHistoryAPI();
                }
                catch (Exception ex)
                {
                    Log.Error($"ExecuteScheduleReadFileHistoryAPI : {ex.Message}");
                }
            }
            finally
            {
                semaphoreCustomer.Release();
            }
            return Task.CompletedTask;
        }

        public void ReadFileHistoryAPI()
        {
            string newFilePath = "";
            string filePathOld = "";
            string content = "";
            try
            {
                string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogAPI);
                if (!Directory.Exists(path) || !Directory.GetFiles(path).Any())
                {
                    return;
                }

                var mostRecentFile = Directory.GetFiles(path)
                    .Select(file => new FileInfo(file))
                    .OrderBy(fileInfo => fileInfo.LastWriteTime)
                    .FirstOrDefault();

                int valueSecond = _commonData.ShowSecond();
                if (mostRecentFile == null || mostRecentFile.Name.Contains(DateTime.Now.ToString("yyyyMMddHHmm_") + valueSecond))
                {
                    return;
                }

                string newFolderPath = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryMoveFolderLogAPI);
                _commonData.CreateFolder1(newFolderPath);

                newFilePath = Path.Combine(newFolderPath, mostRecentFile.Name);
                filePathOld = mostRecentFile.FullName;
                File.Move(mostRecentFile.FullName, newFilePath);

                using (var reader = new StreamReader(newFilePath))
                {
                    content = reader.ReadToEnd().TrimEnd(',');
                }

                content = $"{{\"Data\":[{content}]}}";

                DataFileHistoryAPI? aPIThirdPartyModel = JsonConvert.DeserializeObject<DataFileHistoryAPI>(content);
                if (aPIThirdPartyModel == null || aPIThirdPartyModel.Data == null)
                {
                    File.Delete(newFilePath);
                    return;
                }

                for (int i = 0; i < aPIThirdPartyModel.Data.Count; i += batchSize)
                {
                    var batch = aPIThirdPartyModel.Data.Skip(i).Take(batchSize).ToList();
                    CreateUpdateHistoryAPI(batch);
                }
                File.Delete(newFilePath);
            }
            catch (Exception ex1)
            {
                if (File.Exists(filePathOld) && File.Exists(newFilePath))
                {
                    try
                    {
                        File.Delete(newFilePath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Delete ReadFileHistoryAPI: {ex.Message} \n path: {filePathOld}");
                    }
                }
                Log.Error($"ReadFileHistoryAPI: {ex1.Message}");
            }
        }
        public void CreateUpdateHistoryAPI(List<APIThirdPartyModel> recordsBatchAPI)
        {
            try
            {
                if (recordsBatchAPI == null || !recordsBatchAPI.Any())
                {
                    return;
                }

                var groupedRecords = recordsBatchAPI
                    .Where(r => r.TenantId != null)
                    .GroupBy(r => new
                    {
                        r.TenantId,
                        Date = r.CreatedDate.Date,
                        Minute = new DateTime(r.CreatedDate.Year, r.CreatedDate.Month, r.CreatedDate.Day, r.CreatedDate.Hour, r.CreatedDate.Minute, 0)
                    });

                foreach (var group in groupedRecords)
                {
                    string tenantId = group.Key.TenantId;
                    DateTime createdDate = group.Key.Date;
                    DateTime createdMinute = group.Key.Minute;

                    string pathNow = DataPath.CombineWithRuntimeRoot(DataPath.DataBaseLogIBox, tenantId);
                    _commonData.CreateFolder1(pathNow);
                    if (!Directory.Exists(pathNow))
                    {
                        continue;
                    }

                    string fileDBChatBotNow1 = "";
                    if (createdDate == DateTime.Now.Date)
                    {
                        fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                    }
                    else
                    {
                        fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now.AddDays(-1));
                    }

                    _commonData.CreateFolder1(fileDBChatBotNow1);
                    string PathdbDayNow1 = _iCreateDB.CreateDBSQLiteHistorySizeTime(fileDBChatBotNow1, createdMinute);

                    if (File.Exists(PathdbDayNow1))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(PathdbDayNow1))
                        {
                            using (var connection = dbContextDaily.Database.GetDbConnection())
                            {
                                connection.Open();

                                using (var pragmaCommand = connection.CreateCommand())
                                {
                                    pragmaCommand.CommandText = @"
                                        PRAGMA journal_mode = WAL;          
                                        PRAGMA synchronous = NORMAL;       
                                        PRAGMA cache_size = 1000000;       
                                        PRAGMA temp_store = MEMORY;      
                                        PRAGMA locking_mode = NORMAL;    
                                        PRAGMA busy_timeout = 5000;        
                                    ";
                                    pragmaCommand.ExecuteNonQuery();
                                }

                                using (var transaction = connection.BeginTransaction())
                                {
                                    using (var command = connection.CreateCommand())
                                    {
                                        command.Transaction = transaction;
                                        var querry = new StringBuilder(@"INSERT INTO H_ApiThirdPartyExecuteHistories 
                                        (Id, Wfid, Reason, KeyExecuteRunApiThirdParty, Status, StatusCode, Url, Headers, Request, Response, SiteRun, CreatedDate, IsDelete, ModificationDate, StepName, StepId, WfName, Hour) 
                                        VALUES ");

                                        foreach (var record in group)
                                        {
                                            var id = Guid.NewGuid().ToString();
                                            querry.AppendFormat(@"('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', {12}, '{13}', '{14}', '{15}', '{16}', {17}),",
                                                _commonData.formatDataStringSQL(id),
                                                _commonData.formatDataStringSQL(record.Wfid),
                                                _commonData.formatDataStringSQL(record.Reason),
                                                _commonData.formatDataStringSQL(record.KeyExecuteRunApiThirdParty),
                                                _commonData.formatDataStringSQL(record.Status),
                                                _commonData.formatDataStringSQL(record.StatusCode),
                                                _commonData.formatDataStringSQL(record.Url),
                                                _commonData.formatDataStringSQL(record.Headers),
                                                _commonData.formatDataStringSQL(record.Request),
                                                _commonData.formatDataStringSQL(record.Response),
                                                _commonData.formatDataStringSQL(record.SiteRun),
                                                _commonData.ConvertDateTimeToTimeSpan(record.CreatedDate),
                                                0,
                                                record.ModificationDate.HasValue ? _commonData.ConvertDateTimeToTimeSpan(record.ModificationDate.Value) : _commonData.ConvertDateTimeToTimeSpan(DateTime.Now),
                                                _commonData.formatDataStringSQL(record.StepName),
                                                _commonData.formatDataStringSQL(record.StepId),
                                                _commonData.formatDataStringSQL(record.WFName),
                                                record.CreatedDate.Hour);
                                        }

                                        querry.Length--; // Xóa dấu phẩy cuối cùng

                                        querry.Append(" ON CONFLICT(KeyExecuteRunApiThirdParty) DO UPDATE SET ");
                                        querry.Append(@"Wfid = excluded.Wfid,
                                        Reason = CASE WHEN excluded.Reason IS NOT NULL AND excluded.Reason <> '' THEN excluded.Reason ELSE Reason END,
                                        Status = CASE WHEN excluded.Status = 'Running' THEN Status ELSE excluded.Status END,
                                        StatusCode = excluded.StatusCode,
                                        Url = excluded.Url,
                                        Headers = excluded.Headers,
                                        Request = CASE WHEN excluded.Request IS NOT NULL AND excluded.Request <> '' THEN excluded.Request ELSE Request END,
                                        Response = CASE WHEN excluded.Response IS NOT NULL AND excluded.Response <> '' THEN excluded.Response ELSE Response END,
                                        SiteRun = excluded.SiteRun,
                                        ModificationDate = excluded.ModificationDate,
                                        StepName = excluded.StepName,
                                        WfName = excluded.WfName,
                                        StepId = excluded.StepId;");

                                        _createDB.ExecuteSQLite(command, querry.ToString());
                                    }

                                    transaction.Commit();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateUpdateHistoryAPI: {ex.Message}");
                throw;
            }
        }
    }
}