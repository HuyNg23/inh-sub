using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using IBox.ChatBot.HandleScheduleSQL;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace IBox.History.DB.Schedule.HistoryIBox
{
    [DisallowConcurrentExecution]
    public class ExecuteScheduleUpdateHistoricalResultsDB : IJob
    {
        private readonly DBHistoryContextFactory _historyContextFactory;
        private readonly ICreateDB _createDB;
        private readonly ICommonData _commonData;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public ExecuteScheduleUpdateHistoricalResultsDB(DBHistoryContextFactory historyContextFactory, ICreateDB createDB, ICommonData commonData, Common.Objects.IConfiguration configuration, IEncryption encryption)
        {
            _historyContextFactory = historyContextFactory;
            _createDB = createDB;
            _commonData = commonData;
            _configuration = configuration;
            _encryption = encryption;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                foreach (var tenant in IBGlobalConfig.Tenants)
                {
                    ReadCountLogAndZipFileDay(tenant.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ExecuteScheduleReadFileWF : {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public void ReadCountLogAndZipFileDay(string tenantId)
        {
            //Đọc db và lưu kết quả vào sqlserver
            CheckDataDB_SQLite_And_InsertDB_SQLServer(tenantId);
            //Kiểm tra appsetting config số lượng cần zip file chuyển vào thư mục tạm
            //(ZipFileDay: 2 tính từ ngày hiện tại trừ đi 2 ngày các ngày từ 2 ngày trở đi sẽ được zip file)
            CopyZipFileDay(tenantId);
            //Kiểm tra appsetting config số lượng chứa file zip theo tuần tối đa để zip tiếp và chuyển vào thư mục chung
            //CheckZipFileWeekCount(tenantId);
            //Xóa file Zip quá thời gian cho phép
            RemoveFileZip(tenantId);
        }
        public void RemoveFileZip(string tenantId)
        {
            try
            {
                //string destinationZipPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogMonth, tenantId);
                string destinationZipPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDay, tenantId);
                if (!Directory.Exists(destinationZipPath))
                {
                    return;
                }

                var zipFiles = Directory.GetFiles(destinationZipPath, "*.zip");
                int maxZipFiles = _configuration.Config.Value.MaxZipFileCount;
                if (zipFiles.Length > maxZipFiles)
                {
                    var filesToDelete = zipFiles
                        .Select(file => new FileInfo(file))
                        .OrderBy(file => file.CreationTimeUtc)
                        .Take(zipFiles.Length - maxZipFiles);

                    foreach (var file in filesToDelete)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error Xóa file zip month: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RemoveFileZip: {ex.Message}");
            }
        }
        public void CopyZipFileDay(string tenantId)
        {
            try
            {
                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, tenantId);
                List<string> directories = new List<string>();
                string path = "";
                for (int i = _configuration.Config.Value.ZipFileDay; i < 6; i++)
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

                try
                {
                    string pathDBZip = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDay, tenantId);
                    string zipFileName = $"BackupHistory{path}.zip";
                    string zipFilePath = Path.Combine(pathDBZip, zipFileName);
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

                //Kiểm tra các file trong thư mục move > 3 ngày thì xóa
                List<string> lstPathMove = new List<string>()
                {
                    Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolderLogAPI),
                    Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolderLogWF),
                    Path.Combine(Directory.GetCurrentDirectory(), DataPath.TemporaryMoveFolderLogQuerySQL)
                };
                foreach (var folderPathMove in lstPathMove)
                {
                    if (!Directory.Exists(folderPathMove) || !Directory.GetFiles(folderPathMove).Any())
                    {
                        return;
                    }

                    DateTime threeDaysAgo = DateTime.Now.AddDays(-3);
                    var oldFiles = Directory.GetFiles(folderPathMove)
                         .Select(file => new FileInfo(file))
                         .Where(fileInfo => fileInfo.LastWriteTime < threeDaysAgo)
                         .ToList();

                    foreach (var file in oldFiles)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Delete File Move > 3 day: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CheckZipFileDay: {ex.Message}");
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

        public void CheckZipFileWeekCount(string tenantId)
        {
            try
            {
                var today = DateTime.Today;
                string pathDBZip = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDay, tenantId);
                string destinationZipPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogMonth, tenantId);

                if (!Directory.Exists(pathDBZip) || !Directory.GetFiles(pathDBZip).Any())
                {
                    return;
                }

                var datePattern = new Regex(@"_(\d{8})", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                _commonData.CreateFolder1(destinationZipPath);

                var zipFiles = Directory.GetFiles(pathDBZip, "*.zip")
                    .Select(file =>
                    {
                        var match = datePattern.Match(Path.GetFileNameWithoutExtension(file));
                        if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        {
                            return new { FileInfo = new FileInfo(file), Date = date };
                        }
                        return null;
                    })
                    .Where(x => x != null)
                    .ToList();

                if (zipFiles.Count == 0)
                {
                    Console.WriteLine("Không có file ZIP nào trong thư mục nguồn.");
                    return;
                }
                var calendar = CultureInfo.CurrentCulture.Calendar;
                var currentWeek = calendar.GetWeekOfYear(today, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                var currentYear = today.Year;

                var filesByWeek = zipFiles.GroupBy(file =>
                {
                    var calendar = CultureInfo.CurrentCulture.Calendar;
                    return (Year: file.Date.Year, Week: calendar.GetWeekOfYear(file.Date, CalendarWeekRule.FirstDay, DayOfWeek.Monday));
                });

                foreach (var weekGroup in filesByWeek)
                {
                    var groupYear = weekGroup.Key.Year;
                    var groupWeek = weekGroup.Key.Week;
                    if (groupYear == currentYear && groupWeek == currentWeek)
                    {
                        continue;
                    }

                    if (groupYear == currentYear && groupWeek == currentWeek - 1)
                    {
                        if (today.DayOfWeek == DayOfWeek.Monday || today.DayOfWeek == DayOfWeek.Tuesday)
                        {
                            continue;
                        }
                    }

                    var dateList = weekGroup.Select(file => file.Date.ToString("yyyyMMdd")).OrderBy(d => d).ToList();

                    string zipFileName = $"BackupHistory_{string.Join("_", dateList)}.zip";
                    string zipFilePath = Path.Combine(destinationZipPath, zipFileName);

                    string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    _commonData.CreateFolder1(tempFolder);

                    try
                    {
                        foreach (var file in weekGroup)
                        {
                            string destFile = Path.Combine(tempFolder, file.FileInfo.Name);
                            File.Copy(file.FileInfo.FullName, destFile);
                        }

                        ZipFile.CreateFromDirectory(tempFolder, zipFilePath);
                        foreach (var file in weekGroup)
                        {
                            file.FileInfo.Delete();
                        }
                    }
                    finally
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CheckZipFileCountMonth: {ex.Message}");
            }
        }
        public void CheckDataDB_SQLite_And_InsertDB_SQLServer(string tenantId)
        {
            try
            {
                Check_Update_API(tenantId);
                Check_Update_WF(tenantId);
                Check_Update_SQL(tenantId);
            }
            catch (Exception ex)
            {
                Log.Error($"CheckDataDB_SQLite_And_InsertDB_SQLServer: {ex.Message}");
            }
        }

        public void Check_Update_API(string tenantId)
        {
            try
            {
                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, tenantId);
                _commonData.CreateFolder1(pathNow);

                if (!Directory.Exists(pathNow)) return;

                string fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now.AddDays(-1));

                if (!Directory.Exists(fileDBChatBotNow1)) return;

                var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext = tContext1.GetTenantContext(tenantId).Context;
                // Xử lý dữ liệu theo ngày
                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                string site = IBGlobalConfig.ThisSite;

                ProcessHistoryByDayCommon(tenantContext, fileDBChatBotNow1, TypeHistory.API);

                ProcessHistoryByMonthAPI(tenantContext, fileDBChatBotNow1);

                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Check_Update_API: {ex.Message}, \n tenantId: {tenantId}");
            }
        }
        public void Check_Update_SQL(string tenantId)
        {
            try
            {
                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, tenantId);
                _commonData.CreateFolder1(pathNow);

                if (!Directory.Exists(pathNow)) return;

                string fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now.AddDays(-1));

                if (!Directory.Exists(fileDBChatBotNow1)) return;
                var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext = tContext1.GetTenantContext(tenantId).Context;
                // Xử lý dữ liệu theo ngày

                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                string site = IBGlobalConfig.ThisSite;

                ProcessHistoryByDayCommon(tenantContext, fileDBChatBotNow1, TypeHistory.SQL);

                ProcessHistoryByMonth_SQL(tenantContext, fileDBChatBotNow1);

                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Check_Update_SQL: {ex.Message}, \ntenantId: {tenantId}");
            }
        }
        public void Check_Update_WF(string tenantId)
        {
            try
            {
                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, tenantId);
                _commonData.CreateFolder1(pathNow);

                if (!Directory.Exists(pathNow)) return;

                string fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now.AddDays(-1));

                if (!Directory.Exists(fileDBChatBotNow1)) return;
                var tContext1 = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                var tenantContext = tContext1.GetTenantContext(tenantId).Context;
                // Xử lý dữ liệu theo ngày

                string date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                string site = IBGlobalConfig.ThisSite;

                ProcessHistoryByDayCommon(tenantContext, fileDBChatBotNow1, TypeHistory.WF);

                ProcessHistoryByMonth_WF(tenantContext, fileDBChatBotNow1);

                tenantContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Check_Update_WF: {ex.Message}, \ntenantId: {tenantId}");
            }
        }

        private void ProcessHistoryByDayCommon(TenantContext tenantContext, string databasePath, TypeHistory typeHistory)
        {
            try
            {
                List<SQLite_HistoryDay> sQLite_HistoryDays = new List<SQLite_HistoryDay>();
                var fileDB = Directory.Exists(databasePath) ? Directory.GetFiles(databasePath, "history*.db").ToList() : new List<string>();
                foreach (var filePath in fileDB)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                    {
                        // Chọn bảng theo loại (API, WF, SQL)
                        IQueryable<dynamic>? siteHourSummary = null;

                        switch (typeHistory)
                        {
                            case TypeHistory.API:
                                siteHourSummary = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                                    .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                               || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                                    .GroupBy(ptr => new { ptr.SiteRun, Hour = ptr.Hour })
                                    .Select(group => new
                                    {
                                        SiteRun = group.Key.SiteRun,
                                        Hour = group.Key.Hour,
                                        CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                        CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                                    });
                                break;

                            case TypeHistory.WF:
                                siteHourSummary = dbContextDaily.Context.H_WorkflowExecuteHistories
                                    .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                               || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                                    .GroupBy(ptr => new { ptr.SiteRun, Hour = ptr.Hour })
                                    .Select(group => new
                                    {
                                        SiteRun = group.Key.SiteRun,
                                        Hour = group.Key.Hour,
                                        CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                        CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                                    });
                                break;

                            case TypeHistory.SQL:
                                siteHourSummary = dbContextDaily.Context.H_SQLExecuteHistories
                                    .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                               || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                                    .GroupBy(ptr => new { ptr.SiteRun, Hour = ptr.Hour })
                                    .Select(group => new
                                    {
                                        SiteRun = group.Key.SiteRun,
                                        Hour = group.Key.Hour,
                                        CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                        CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                                    });
                                break;
                        }

                        var allHours = Enumerable.Range(0, 24).Select(hour => new SQLite_HistoryDay
                        {
                            SiteRun = IBGlobalConfig.ThisSite,
                            CreatedDate = DateTime.Now,
                            Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                            Hour = hour,
                            Id = Guid.NewGuid().ToString(),
                            ModificationDate = DateTime.Now,
                            Type = typeHistory,
                            Site = IBGlobalConfig.ThisSite,
                            IsDelete = false,
                            CountSuccess = 0,
                            CountFail = 0
                        }).ToList();

                        if (siteHourSummary == null)
                        {
                            return;
                        }

                        var siteHourDict = siteHourSummary.ToDictionary(
                            entry => entry.Hour,
                            entry => new SQLite_HistoryDay
                            {
                                SiteRun = entry.SiteRun,
                                CountFail = entry.CountFail,
                                CountSuccess = entry.CountSuccess,
                                CreatedDate = DateTime.Now,
                                Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                Hour = entry.Hour,
                                Id = Guid.NewGuid().ToString(),
                                IsDelete = false,
                                ModificationDate = DateTime.Now,
                                Type = typeHistory,
                                Site = IBGlobalConfig.ThisSite
                            }
                        );

                        sQLite_HistoryDays = allHours.Select(hourData =>
                        {
                            if (siteHourDict.TryGetValue(hourData.Hour, out var summaryData))
                            {
                                return summaryData;
                            }
                            return hourData;
                        }).ToList();
                    }
                }

                foreach (var history in sQLite_HistoryDays)
                {
                    if (!tenantContext.Context.SQLite_HistoryDays
                        .Any(h => h.SiteRun == history.SiteRun
                                          && h.Date == history.Date
                                          && h.Hour == history.Hour
                                          && h.Type == typeHistory))
                    {
                        tenantContext.Context.SQLite_HistoryDays.Add(history);
                    }
                }

                if (typeHistory == TypeHistory.SQL)
                {
                    var oldRecords = tenantContext.Context.SQLite_HistoryDays
                      .Where(ptr => ptr.CreatedDate <= DateTime.Now.AddDays(-60))
                      .ToList();
                    tenantContext.Context.SQLite_HistoryDays.RemoveRange(oldRecords);
                }

                tenantContext.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"ProcessHistoryByDayCommon: {ex.Message}");
            }
        }
        public static class DbFunctions
        {
            [DbFunction("ConvertTimeSpanToDateTime", "dbo")]
            public static DateTime ConvertTimeSpanToDateTime(long timespan)
            {
                return DateTimeOffset.FromUnixTimeSeconds(timespan).UtcDateTime.AddHours(7);
            }
        }

        private void ProcessHistoryByMonthAPI(TenantContext tenantContext, string databasePath)
        {
            try
            {
                var fileDB = Directory.Exists(databasePath) ? Directory.GetFiles(databasePath, "history*.db").ToList() : new List<string>();
                var summaryDict = new Dictionary<string, (int Success, int Fail)>();
                foreach (var filePath in fileDB)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                    {
                        var siteDaySummary = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                            .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                       || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                            .GroupBy(ptr => ptr.SiteRun)
                            .Select(group => new
                            {
                                SiteRun = group.Key,
                                CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                            }).ToList();

                        foreach (var entry in siteDaySummary)
                        {
                            if (summaryDict.ContainsKey(entry.SiteRun))
                            {
                                var current = summaryDict[entry.SiteRun];
                                summaryDict[entry.SiteRun] = (current.Success + entry.CountSuccess, current.Fail + entry.CountFail);
                            }
                            else
                            {
                                summaryDict[entry.SiteRun] = (entry.CountSuccess, entry.CountFail);
                            }
                        }
                    }

                    var historyMonths = summaryDict != null && summaryDict.Count > 0
                          ? summaryDict.Select(entry => new SQLite_HistoryMonth
                          {
                              SiteRun = entry.Key,
                              CountFail = entry.Value.Fail,
                              CountSuccess = entry.Value.Success,
                              CreatedDate = DateTime.Now,
                              DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                              Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                              Id = Guid.NewGuid().ToString(),
                              IsDelete = false,
                              ModificationDate = DateTime.Now,
                              Type = TypeHistory.API,
                              Site = IBGlobalConfig.ThisSite
                          }).ToList()
                          : new List<SQLite_HistoryMonth>()
                          {
                                new SQLite_HistoryMonth()
                                {
                                    SiteRun = IBGlobalConfig.ThisSite,
                                    CountFail = 0,
                                    CountSuccess = 0,
                                    CreatedDate = DateTime.Now,
                                    DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                                    Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                    Id = Guid.NewGuid().ToString(),
                                    IsDelete = false,
                                    ModificationDate = DateTime.Now,
                                    Type = TypeHistory.API,
                                    Site = IBGlobalConfig.ThisSite,
                                }
                          };

                    foreach (var history in historyMonths)
                    {
                        if (!tenantContext.Context.SQLite_HistoryMonths
                            .Any(h => h.SiteRun == history.SiteRun
                                              && h.Date == history.Date))
                        {
                            tenantContext.Context.SQLite_HistoryMonths.Add(history);
                        }
                    }

                    tenantContext.Context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ProcessHistoryByMonth: {ex.Message}");
            }
        }
        private void ProcessHistoryByMonth_WF(TenantContext tenantContext, string databasePath)
        {
            try
            {
                var fileDB = Directory.Exists(databasePath) ? Directory.GetFiles(databasePath, "history*.db").ToList() : new List<string>();
                var summaryDict = new Dictionary<string, (int Success, int Fail)>();
                foreach (var filePath in fileDB)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                    {
                        var siteDaySummary = dbContextDaily.Context.H_WorkflowExecuteHistories
                            .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                       || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                            .GroupBy(ptr => ptr.SiteRun)
                            .Select(group => new
                            {
                                SiteRun = group.Key,
                                CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                            }).ToList();

                        foreach (var entry in siteDaySummary)
                        {
                            if (summaryDict.ContainsKey(entry.SiteRun))
                            {
                                var current = summaryDict[entry.SiteRun];
                                summaryDict[entry.SiteRun] = (current.Success + entry.CountSuccess, current.Fail + entry.CountFail);
                            }
                            else
                            {
                                summaryDict[entry.SiteRun] = (entry.CountSuccess, entry.CountFail);
                            }
                        }
                    }
                }

                var historyMonths = summaryDict != null && summaryDict.Count > 0
                                 ? summaryDict.Select(entry => new SQLite_HistoryMonth
                                 {
                                     SiteRun = entry.Key,
                                     CountFail = entry.Value.Fail,
                                     CountSuccess = entry.Value.Success,
                                     CreatedDate = DateTime.Now,
                                     DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                                     Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                     Id = Guid.NewGuid().ToString(),
                                     IsDelete = false,
                                     ModificationDate = DateTime.Now,
                                     Type = TypeHistory.WF,
                                     Site = IBGlobalConfig.ThisSite
                                 }).ToList()
                                 : new List<SQLite_HistoryMonth>
                                 {
                                    new SQLite_HistoryMonth
                                    {
                                        SiteRun = IBGlobalConfig.ThisSite,
                                        CountFail = 0,
                                        CountSuccess = 0,
                                        CreatedDate = DateTime.Now,
                                        DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                                        Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                        Id = Guid.NewGuid().ToString(),
                                        IsDelete = false,
                                        ModificationDate = DateTime.Now,
                                        Type = TypeHistory.WF,
                                        Site = IBGlobalConfig.ThisSite
                                    }
                                 };

                foreach (var history in historyMonths)
                {
                    if (!tenantContext.Context.SQLite_HistoryMonths
                        .Any(h => h.SiteRun == history.SiteRun
                                          && h.Date == history.Date
                                          && h.Type == TypeHistory.WF))
                    {
                        tenantContext.Context.SQLite_HistoryMonths.Add(history);
                    }
                }

                tenantContext.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"ProcessHistoryByMonth_WF: {ex.Message}");
            }
        }
        private void ProcessHistoryByMonth_SQL(TenantContext tenantContext, string databasePath)
        {
            try
            {
                var fileDB = Directory.Exists(databasePath) ? Directory.GetFiles(databasePath, "history*.db").ToList() : new List<string>();
                var summaryDict = new Dictionary<string, (int Success, int Fail)>();

                foreach (var filePath in fileDB)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                    {
                        var siteDaySummary = dbContextDaily.Context.H_SQLExecuteHistories
                            .Where(ptr => ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                       || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                            .GroupBy(ptr => ptr.SiteRun)
                            .Select(group => new
                            {
                                SiteRun = group.Key,
                                CountFail = group.Count(e => e.Status == WorkflowExecuteStatus.Fail.ToString()),
                                CountSuccess = group.Count(e => e.Status == WorkflowExecuteStatus.Complete.ToString())
                            }).ToList();
                        foreach (var entry in siteDaySummary)
                        {
                            if (summaryDict.ContainsKey(entry.SiteRun))
                            {
                                var current = summaryDict[entry.SiteRun];
                                summaryDict[entry.SiteRun] = (current.Success + entry.CountSuccess, current.Fail + entry.CountFail);
                            }
                            else
                            {
                                summaryDict[entry.SiteRun] = (entry.CountSuccess, entry.CountFail);
                            }
                        }
                    }
                }
                var historyMonths = summaryDict != null && summaryDict.Count > 0
                            ? summaryDict.Select(entry => new SQLite_HistoryMonth
                            {
                                SiteRun = entry.Key,
                                CountFail = entry.Value.Fail,
                                CountSuccess = entry.Value.Success,
                                CreatedDate = DateTime.Now,
                                DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                                Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                Id = Guid.NewGuid().ToString(),
                                IsDelete = false,
                                ModificationDate = DateTime.Now,
                                Type = TypeHistory.SQL,
                                Site = IBGlobalConfig.ThisSite
                            }).ToList()
                            : new List<SQLite_HistoryMonth>
                            {
                                new SQLite_HistoryMonth
                                {
                                    SiteRun = IBGlobalConfig.ThisSite,
                                    CountFail = 0,
                                    CountSuccess = 0,
                                    CreatedDate = DateTime.Now,
                                       DateMonth = DateTime.Now.AddDays(-1).ToString("yyyy-MM"),
                                        Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                                    Id = Guid.NewGuid().ToString(),
                                    IsDelete = false,
                                    ModificationDate = DateTime.Now,
                                    Type = TypeHistory.SQL,
                                    Site = IBGlobalConfig.ThisSite
                                }
                            };

                foreach (var history in historyMonths)
                {
                    if (!tenantContext.Context.SQLite_HistoryMonths
                        .Any(h => h.SiteRun == history.SiteRun
                                          && h.Date == history.Date
                                          && h.Type == TypeHistory.SQL))
                    {
                        tenantContext.Context.SQLite_HistoryMonths.Add(history);
                    }
                }

                var sqliteHistoryMonth = tenantContext.Context.SQLite_HistoryMonths
                    .Where(ptr => ptr.CreatedDate <= DateTime.Now.AddDays(-60))
                    .ToList();
                var historyJob = tenantContext.Context.S_ImplementationHistorys
                    .Where(ptr => ptr.CreatedDate <= DateTime.Now.AddDays(-60))
                    .ToList();
                tenantContext.Context.SQLite_HistoryMonths.RemoveRange(sqliteHistoryMonth);
                tenantContext.Context.S_ImplementationHistorys.RemoveRange(historyJob);

                tenantContext.Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error($"ProcessHistoryByMonth_SQL: {ex.Message}");
            }
        }

    }
}