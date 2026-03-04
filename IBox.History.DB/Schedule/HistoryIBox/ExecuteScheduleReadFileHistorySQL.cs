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
    public class ExecuteScheduleReadFileHistorySQL : IJob
    {
        private readonly DBHistoryContextFactory _historyContextFactory;
        private readonly ICreateDB _createDB;
        private readonly ICreateDB _iCreateDB;
        private readonly ICommonData _commonData;
        private static readonly SemaphoreSlim semaphoreCustomer = new SemaphoreSlim(2);
        private const int batchSize = 500;

        public ExecuteScheduleReadFileHistorySQL(DBHistoryContextFactory historyContextFactory, ICreateDB createDB, ICommonData commonData, ICreateDB iCreateDB)
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
                    ReadFileHistorySQL();
                }
                catch (Exception ex)
                {
                    Log.Error($"ExecuteScheduleReadFileHistorySQL : {ex.Message}");
                }
            }
            finally
            {
                semaphoreCustomer.Release();
            }
            return Task.CompletedTask;
        }

        public void ReadFileHistorySQL()
        {
            string newFilePath = "";
            string filePathOld = "";
            string content = "";
            try
            {
                string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogQuerySQL);
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

                string newFolderPath = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryMoveFolderLogQuerySQL);
                _commonData.CreateFolder1(newFolderPath);

                newFilePath = Path.Combine(newFolderPath, mostRecentFile.Name);
                filePathOld = mostRecentFile.FullName;
                File.Move(mostRecentFile.FullName, newFilePath);

                using (var reader = new StreamReader(newFilePath))
                {
                    content = reader.ReadToEnd().TrimEnd(',');
                }

                content = $"{{\"Data\":[{content}]}}";

                DataFileSQL? sqlModel = JsonConvert.DeserializeObject<DataFileSQL>(content);
                if (sqlModel == null || sqlModel.Data == null)
                {
                    File.Delete(newFilePath);
                    return;
                }
                for (int i = 0; i < sqlModel.Data.Count; i += batchSize)
                {
                    var batch = sqlModel.Data.Skip(i).Take(batchSize).ToList();
                    CreateUpdateHistorySQL(batch);
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
                        Log.Error($"Delete ReadFileHistorySQL: {ex.Message} \n path: {filePathOld}");
                    }
                }
                Log.Error($"ReadFileHistoryAPI: {ex1.Message}");
            }
        }

        public void CreateUpdateHistorySQL(List<ExecuteQuerySQLModel> recordsBatchSQL)
        {
            try
            {
                if (recordsBatchSQL == null || !recordsBatchSQL.Any())
                {
                    return;
                }

                var groupedRecords = recordsBatchSQL
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
                        return;
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
                                        var querry = new StringBuilder(@"INSERT INTO H_SQLExecuteHistories ( Id, CreatedDate, IsDelete, ModificationDate, WfId, WfName, Reason, KeyExecuteRunQuerySQL, Status, Query, RecordCount, SiteRun, StepName, StepId, Address, Catalog, Hour)
                                    VALUES ");

                                        foreach (var record in group)
                                        {
                                            var id = Guid.NewGuid().ToString();
                                            querry.AppendFormat(@"( '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}', '{15}', '{16}'),",
                                            _commonData.formatDataStringSQL(id),
                                            _commonData.ConvertDateTimeToTimeSpan(record.CreatedDate),
                                            0,
                                            record.ModificationDate != null ? _commonData.ConvertDateTimeToTimeSpan(record.ModificationDate.Value) : _commonData.ConvertDateTimeToTimeSpan(record.CreatedDate),
                                            _commonData.formatDataStringSQL(record.WfId),
                                            _commonData.formatDataStringSQL(record.WFName),
                                            _commonData.formatDataStringSQL(record.Reason),
                                            _commonData.formatDataStringSQL(record.KeyExecuteRunQuerySQL),
                                            _commonData.formatDataStringSQL(record.Status),
                                            _commonData.formatDataStringSQL(record.Query),
                                            record.RecordCount,
                                            _commonData.formatDataStringSQL(record.SiteRun),
                                             _commonData.formatDataStringSQL(record.StepName),
                                            _commonData.formatDataStringSQL(record.StepId),
                                            _commonData.formatDataStringSQL(record.Address),
                                            _commonData.formatDataStringSQL(record.Catalog),
                                            record.CreatedDate.Hour);
                                        }
                                        querry.Length--;
                                        querry.Append(" ON CONFLICT(KeyExecuteRunQuerySQL) DO UPDATE SET ");
                                        querry.Append(@"ModificationDate = CASE WHEN excluded.ModificationDate IS NOT NULL THEN excluded.ModificationDate ELSE ModificationDate END,
                                            WfId = excluded.Wfid,
                                            WfName = excluded.WfName,
                                            Reason = CASE WHEN excluded.Reason IS NOT NULL AND excluded.Reason <> '' THEN excluded.Reason ELSE Reason END,
                                            Status = CASE WHEN excluded.Status = 'Running' then Status else excluded.Status END,
                                            Query = excluded.Query,
                                            RecordCount = excluded.RecordCount,
                                            SiteRun = excluded.SiteRun,
                                            StepName = excluded.StepName,
                                            StepId = excluded.StepId,
                                            Address = excluded.Address,
                                            Catalog = excluded.Catalog;");

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
                Log.Error($"CreateUpdateHistorySQL: {ex.Message}");
                throw;
            }
        }
    }
}