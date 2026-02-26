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
    public class ExecuteScheduleReadFileHistoryWF : IJob
    {
        private readonly DBHistoryContextFactory _historyContextFactory;
        private readonly ICreateDB _createDB;
        private readonly ICreateDB _iCreateDB;
        private readonly ICommonData _commonData;
        private static readonly SemaphoreSlim semaphoreCustomer = new SemaphoreSlim(2);
        private const int batchSize = 500;


        public ExecuteScheduleReadFileHistoryWF(DBHistoryContextFactory historyContextFactory, ICreateDB createDB, ICommonData commonData, ICreateDB iCreateDB)
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
                    ReadFileHistoryWF();
                }
                catch (Exception ex)
                {
                    Log.Error($"ExecuteScheduleReadFileWF : {ex.Message}");
                }
            }
            finally
            {
                semaphoreCustomer.Release();
            }

            return Task.CompletedTask;
        }

        public void ReadFileHistoryWF()
        {
            string newFilePath = "";
            string filePathOld = "";
            string content = "";
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryFolderLogWF);
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

                string newFolderPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolderLogWF);
                _commonData.CreateFolder1(newFolderPath);

                newFilePath = Path.Combine(newFolderPath, mostRecentFile.Name);
                filePathOld = mostRecentFile.FullName;
                File.Move(mostRecentFile.FullName, newFilePath);

                using (var reader = new StreamReader(newFilePath))
                {
                    content = reader.ReadToEnd().TrimEnd(',');
                }

                content = $"{{\"Data\":[{content}]}}";

                DataFileWF? wfThirdPartyModel = JsonConvert.DeserializeObject<DataFileWF>(content);
                if (wfThirdPartyModel == null || wfThirdPartyModel.Data == null)
                {
                    File.Delete(newFilePath);
                    return;
                }

                for (int i = 0; i < wfThirdPartyModel.Data.Count; i += batchSize)
                {
                    var batch = wfThirdPartyModel.Data.Skip(i).Take(batchSize).ToList();
                    CreateUpdateHistoryWF(batch);
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
                        Log.Error($"Delete ReadFileHistoryWF: {ex.Message} \n path: {filePathOld}");
                    }
                }
                Log.Error($"ReadFileHistoryWF: {ex1}\n DataFileHistoryWF: {content}");
            }
        }

        public void CreateUpdateHistoryWF(List<WFModel> recordsBatchWF)
        {
            try
            {
                if (recordsBatchWF == null || !recordsBatchWF.Any())
                {
                    return;
                }

                var groupedRecords = recordsBatchWF
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

                    string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, tenantId);
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

                                        //Kiểm tra và insert hoặc update sqlite
                                        var querry = new StringBuilder(@"INSERT INTO H_WorkflowExecuteHistories ( Id, Wfid, Status, Reason, KeyExecuteRunWorkFlow, SiteRun, Header, RequestWF, ResultWF, CreatedDate, IsDelete, ModificationDate, WfName, Hour)
                                                VALUES ");

                                        foreach (var record in group)
                                        {
                                            var id = Guid.NewGuid().ToString();
                                            querry.AppendFormat(@"('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}'),",
                                                _commonData.formatDataStringSQL(id),
                                                _commonData.formatDataStringSQL(record.Wfid),
                                                _commonData.formatDataStringSQL(record.Status),
                                                _commonData.formatDataStringSQL(record.Reason),
                                                _commonData.formatDataStringSQL(record.KeyExecuteRunWorkFlow),
                                                _commonData.formatDataStringSQL(record.SiteRun),
                                                _commonData.formatDataStringSQL(record.Header),
                                                _commonData.formatDataStringSQL(record.RequestWF),
                                                _commonData.formatDataStringSQL(record.ResultWF),
                                                _commonData.ConvertDateTimeToTimeSpan(record.CreatedDate),
                                                0,
                                                record.ModificationDate != null ? _commonData.ConvertDateTimeToTimeSpan(record.ModificationDate.Value) : _commonData.ConvertDateTimeToTimeSpan(record.CreatedDate),
                                                _commonData.formatDataStringSQL(record.WFName),
                                                record.CreatedDate.Hour);
                                        }
                                        querry.Length--; // Xóa dấu phẩy cuối cùng
                                        querry.Append(" ON CONFLICT(KeyExecuteRunWorkFlow) DO UPDATE SET ");
                                        querry.Append(@"Wfid = excluded.Wfid,
                                            Status = CASE WHEN excluded.Status = 'Running' then Status else excluded.Status END,
                                            Reason = CASE WHEN excluded.Reason is not null or excluded.Reason <> '' then excluded.Reason else Reason end,
                                            SiteRun = excluded.SiteRun,
                                            Header = excluded.Header,
                                            RequestWF = CASE WHEN excluded.RequestWF is not null or excluded.RequestWF <> '' then excluded.RequestWF else RequestWF end,
                                            ResultWF = CASE WHEN excluded.ResultWF is not null or excluded.ResultWF <> '' then excluded.ResultWF else ResultWF end,
                                            WfName = CASE WHEN excluded.WfName is not null or excluded.WfName <> '' then excluded.WfName else WfName end,
                                            ModificationDate = CASE WHEN excluded.ModificationDate IS NOT NULL THEN excluded.ModificationDate ELSE ModificationDate END;");

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
                Log.Error($"CreateUpdateHistoryWF: {ex.Message}");
                throw;
            }
        }
    }
}