using IBox.ChatBot.HandleScheduleSQL;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.ServiceIB;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using IBox.Database.Tenant;
using IBox.Workflow.Model;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using Serilog;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace IBox.History.DB.WorkflowAndApiThirdPartyHistory
{
    public class HandelWFAndAPI : IHandelWFAndAPI
    {
        public readonly ICommonData _commonData;
        public readonly ICreateDB _createDB;
        public readonly DBHistoryContextFactory _historyContextFactory;
        private readonly IBContext<TenantContext> _tenantContext;
        private readonly IRestAPI _restAPI;
        private List<string> statusList = new List<string> { "Running", "Complete", "Fail" };

        public HandelWFAndAPI(ICommonData commonData, ICreateDB createDB, DBHistoryContextFactory historyContextFactory, IBContext<TenantContext> tenantContext, IRestAPI restAPI)
        {
            _commonData = commonData;
            _createDB = createDB;
            _historyContextFactory = historyContextFactory;
            _tenantContext = tenantContext;
            _restAPI = restAPI;
        }
        public ResponseAllWorkflowExecuteHistory GetAllWorkflowExecuteHistories(RequestGetAllExecuteHistory requestGetAllExecuteHistory)
        {
            try
            {
                if (requestGetAllExecuteHistory == null)
                {
                    return new ResponseAllWorkflowExecuteHistory();
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, requestGetAllExecuteHistory.TenantId ?? "");
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseAllWorkflowExecuteHistory();
                }

                string fileDBChatBotNow1 = "";

                DateTime date = _commonData.ToDate1(requestGetAllExecuteHistory.Date);
                DateTime start = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {requestGetAllExecuteHistory.StartTime}"));
                DateTime end = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {requestGetAllExecuteHistory.EndTime}"));
                long startDate = _commonData.ConvertDateTimeToTimeSpan(start);
                long endDate;
                if (string.IsNullOrEmpty(requestGetAllExecuteHistory.EndTime))
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
                }
                else
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(end);
                }

                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, start);
                Regex dbPattern = new(@"history_(\d{8}_\d{6})\.db$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db") : Array.Empty<string>(); ;

                if (fileDB.Length == 0)
                {
                    fileDB = UnZipFileDB(requestGetAllExecuteHistory.TenantId ?? "", start);
                }

                var dbFiles = fileDB
                    .Select(file => new
                    {
                        Path = file,
                        Match = dbPattern.Match(file),
                        IsDefault = Path.GetFileName(file).Equals("history.db", StringComparison.OrdinalIgnoreCase)
                    })
                    .Select(f => new
                    {
                        f.Path,
                        f.IsDefault,
                        Date = f.Match.Success ? DateTime.ParseExact(f.Match.Groups[1].Value, "yyyyMMdd_HHmmss", null) : DateTime.MinValue
                    })
                    .OrderByDescending(f => f.Date)
                    .ToList();

                var filteredFiles = dbFiles
                    .Where(f => f.Date >= start && f.Date <= end)
                    .ToList();

                if (!filteredFiles.Any() || dbFiles.Any(f => f.Date < start))
                {
                    var extraFile = dbFiles.FirstOrDefault(f => f.Date < start);
                    if (extraFile != null)
                    {
                        filteredFiles.Add(extraFile);
                    }
                }

                int pageSize = requestGetAllExecuteHistory.Take ?? 0;
                int skipCount = requestGetAllExecuteHistory.Skip ?? 0;
                var results = new List<WorkflowExecuteHistory>();
                int totalRecords = 0;

                foreach (var dbPath in filteredFiles)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var countQuery = dbContextDaily.Context.H_WorkflowExecuteHistories
                            .Where(ptr => ptr.CreatedDate >= startDate && ptr.CreatedDate <= endDate
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfName) || (ptr.WfName != null && ptr.WfName.Contains(requestGetAllExecuteHistory.WfName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfId) || ptr.WfId == requestGetAllExecuteHistory.WfId)
                                && (requestGetAllExecuteHistory.Status == null || !statusList.Contains(requestGetAllExecuteHistory.Status.ToString()) || (ptr.Status != null && ptr.Status == requestGetAllExecuteHistory.Status.ToString()))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Request) || (ptr.RequestWF != null && ptr.RequestWF.Contains(requestGetAllExecuteHistory.Request)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Response) || (ptr.ResultWF != null && ptr.ResultWF.Contains(requestGetAllExecuteHistory.Response)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.SiteRun) || (ptr.SiteRun != null && ptr.SiteRun.Contains(requestGetAllExecuteHistory.SiteRun))));

                        totalRecords += countQuery.Count();
                    }
                }

                int currentCount = 0;
                foreach (var dbPath in filteredFiles)
                {
                    if (results.Count >= pageSize) break;

                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var query = dbContextDaily.Context.H_WorkflowExecuteHistories
                            .Where(ptr => ptr.CreatedDate >= startDate && ptr.CreatedDate <= endDate
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfName) || (ptr.WfName != null && ptr.WfName.Contains(requestGetAllExecuteHistory.WfName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfId) || ptr.WfId == requestGetAllExecuteHistory.WfId)
                                && (requestGetAllExecuteHistory.Status == null || !statusList.Contains(requestGetAllExecuteHistory.Status.ToString()) || (ptr.Status != null && ptr.Status == requestGetAllExecuteHistory.Status.ToString()))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Request) || (ptr.RequestWF != null && ptr.RequestWF.Contains(requestGetAllExecuteHistory.Request)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Response) || (ptr.ResultWF != null && ptr.ResultWF.Contains(requestGetAllExecuteHistory.Response)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.SiteRun) || (ptr.SiteRun != null && ptr.SiteRun.Contains(requestGetAllExecuteHistory.SiteRun))))
                            .OrderByDescending(ptr => ptr.CreatedDate);

                        // Bỏ qua bản ghi trong các file trước
                        int remainingSkip = Math.Max(0, skipCount - currentCount);
                        var partialResult = query
                            .Skip(remainingSkip)
                            .Take(pageSize - results.Count)
                            .Select(ptr => new WorkflowExecuteHistory
                            {
                                KeyExecuteRunWorkFlow = ptr.KeyExecuteRunWorkFlow,
                                Reason = ptr.Reason,
                                Header = ptr.Header,
                                RequestWF = ptr.RequestWF,
                                ResultWF = ptr.ResultWF,
                                SiteRun = ptr.SiteRun,
                                Status = ptr.Status == "Complete" ? WorkflowExecuteStatus.Complete :
                                        ptr.Status == "Running" ? WorkflowExecuteStatus.Running :
                                        WorkflowExecuteStatus.Fail,
                                TimeEnd = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate),
                                TimeStart = _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                TimeInterval = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate) - _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                WorkflowId = ptr.WfId,
                                WorkflowName = ptr.WfName,
                            })
                            .ToList();

                        currentCount += query.Count();

                        if (partialResult.Count > 0)
                        {
                            results.AddRange(partialResult);
                        }
                    }
                }

                return new ResponseAllWorkflowExecuteHistory
                {
                    Data = results,
                    TotalReCords = totalRecords
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllWorkflowExecuteHistories: {ex.Message}");
                throw;
            }
        }
        public ResponseAllApiThirdPartyExecuteHistory GetAllApiThirdPartyExecuteHistories(RequestGetAllExecuteThirdPartyHistory req)
        {
            try
            {
                if (req == null)
                {
                    return new ResponseAllApiThirdPartyExecuteHistory();
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, req.TenantId ?? "");
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseAllApiThirdPartyExecuteHistory();
                }

                string fileDBChatBotNow1 = "";

                DateTime date = _commonData.ToDate1(req.Date);
                DateTime start = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {req.StartTime}"));
                DateTime end = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {req.EndTime}"));
                long startDate = _commonData.ConvertDateTimeToTimeSpan(start); ;

                long endDate;
                if (string.IsNullOrEmpty(req.EndTime))
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
                }
                else
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(end);
                }

                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, start);
                Regex dbPattern = new(@"history_(\d{8}_\d{6})\.db$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db") : Array.Empty<string>();

                if (fileDB.Length == 0)
                {
                    fileDB = UnZipFileDB(req.TenantId ?? "", start);
                }

                var dbFiles = fileDB
                    .Select(file => new
                    {
                        Path = file,
                        Match = dbPattern.Match(file),
                        IsDefault = Path.GetFileName(file).Equals("history.db", StringComparison.OrdinalIgnoreCase)
                    })
                    .Select(f => new
                    {
                        f.Path,
                        f.IsDefault,
                        Date = f.Match.Success ? DateTime.ParseExact(f.Match.Groups[1].Value, "yyyyMMdd_HHmmss", null) : DateTime.MinValue
                    })
                    .OrderByDescending(f => f.Date)
                    .ToList();

                var filteredFiles = dbFiles
                    .Where(f => f.Date >= start && f.Date <= end)
                    .ToList();

                if (!filteredFiles.Any() || dbFiles.Any(f => f.Date < start))
                {
                    var extraFile = dbFiles.FirstOrDefault(f => f.Date < start);
                    if (extraFile != null)
                    {
                        filteredFiles.Add(extraFile);
                    }
                }

                int pageSize = req.Take ?? 0;
                int skipCount = req.Skip ?? 0;
                var results = new List<ApiThirdPartyExecuteHistory>();
                int totalRecords = 0;

                foreach (var dbPath in filteredFiles)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var countQuery = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                              .Where(ptr => ptr.CreatedDate >= startDate
                                  && ptr.CreatedDate <= endDate
                                  && (string.IsNullOrEmpty(req.WfId) || (ptr.WfId != null && ptr.WfId.Contains(req.WfId)))
                                  && (string.IsNullOrEmpty(req.WorkflowName) || (ptr.WfName != null && ptr.WfName.Contains(req.WorkflowName)))
                                  && (req.StatusSearch == null || !statusList.Contains(req.StatusSearch.ToString()) || (ptr.Status != null && ptr.Status == req.StatusSearch.ToString()))
                                  && (string.IsNullOrEmpty(req.StatusCode) || (ptr.StatusCode != null && ptr.StatusCode.Contains(req.StatusCode)))
                                  && (string.IsNullOrEmpty(req.Request) || (ptr.Request != null && ptr.Request.Contains(req.Request)))
                                  && (string.IsNullOrEmpty(req.Response) || (ptr.Response != null && ptr.Response.Contains(req.Response)))
                                  && (string.IsNullOrEmpty(req.Url) || (ptr.Url != null && ptr.Url.Contains(req.Url)))
                                  && (string.IsNullOrEmpty(req.StepId) || (ptr.StepId != null && ptr.StepId.Contains(req.StepId)))
                                  && (string.IsNullOrEmpty(req.Site) || (ptr.SiteRun != null && ptr.SiteRun.Contains(req.Site)))
                                  && (string.IsNullOrEmpty(req.StepName) || (ptr.StepName != null && ptr.StepName.Contains(req.StepName))));

                        totalRecords += countQuery.Count();
                    }
                }

                int currentCount = 0;
                foreach (var dbPath in filteredFiles)
                {
                    if (results.Count >= pageSize) break; // Đã đủ số lượng cần lấy

                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var query = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                              .Where(ptr => ptr.CreatedDate >= startDate
                                  && ptr.CreatedDate <= endDate
                                  && (string.IsNullOrEmpty(req.WfId) || (ptr.WfId != null && ptr.WfId.Contains(req.WfId)))
                                  && (string.IsNullOrEmpty(req.WorkflowName) || (ptr.WfName != null && ptr.WfName.Contains(req.WorkflowName)))
                                  && (req.StatusSearch == null || !statusList.Contains(req.StatusSearch.ToString()) || (ptr.Status != null && ptr.Status == req.StatusSearch.ToString()))
                                  && (string.IsNullOrEmpty(req.StatusCode) || (ptr.StatusCode != null && ptr.StatusCode.Contains(req.StatusCode)))
                                  && (string.IsNullOrEmpty(req.Request) || (ptr.Request != null && ptr.Request.Contains(req.Request)))
                                  && (string.IsNullOrEmpty(req.Response) || (ptr.Response != null && ptr.Response.Contains(req.Response)))
                                  && (string.IsNullOrEmpty(req.Url) || (ptr.Url != null && ptr.Url.Contains(req.Url)))
                                  && (string.IsNullOrEmpty(req.StepId) || (ptr.StepId != null && ptr.StepId.Contains(req.StepId)))
                                  && (string.IsNullOrEmpty(req.Site) || (ptr.SiteRun != null && ptr.SiteRun.Contains(req.Site)))
                                  && (string.IsNullOrEmpty(req.StepName) || (ptr.StepName != null && ptr.StepName.Contains(req.StepName))))
                              .OrderByDescending(ptr => ptr.CreatedDate);

                        int remainingSkip = Math.Max(0, skipCount - currentCount);
                        var partialResult = query
                            .Skip(remainingSkip)
                            .Take(pageSize - results.Count)
                            .Select(ptr => new ApiThirdPartyExecuteHistory
                            {
                                Headers = ptr.Headers,
                                Request = ptr.Request,
                                Response = ptr.Response,
                                StepName = ptr.StepName,
                                SiteRun = ptr.SiteRun,
                                StepId = ptr.StepId,
                                Url = ptr.Url,
                                StatusCode = ptr.StatusCode,
                                Reason = ptr.Reason,
                                Status = ptr.Status == "Complete" ? ExecuteRunApiThirdPartyStatus.Complete : ptr.Status == "Running" ? ExecuteRunApiThirdPartyStatus.Running : ExecuteRunApiThirdPartyStatus.Fail,
                                TimeEnd = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate),
                                TimeStart = _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                TimeInterval = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate) - _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                WorkflowId = ptr.WfId,
                                WorkflowName = ptr.WfName,
                            })
                            .ToList();

                        currentCount += query.Count();

                        if (partialResult.Count > 0)
                        {
                            results.AddRange(partialResult);
                        }
                    }
                }

                return new ResponseAllApiThirdPartyExecuteHistory
                {
                    Data = results,
                    TotalReCords = totalRecords
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllApiThirdPartyExecuteHistories: {ex.Message}");
                throw;
            }
        }
        public ResponseGetAllExecuteQuerySQLHistory GetAllQuerySQLExecuteHistories(RequestGetAllExecuteQuerySQLHistory requestGetAllExecuteHistory)
        {
            try
            {
                if (requestGetAllExecuteHistory == null)
                {
                    return new ResponseGetAllExecuteQuerySQLHistory();
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, requestGetAllExecuteHistory.TenantId ?? "");
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseGetAllExecuteQuerySQLHistory();
                }

                string fileDBChatBotNow1 = "";

                DateTime date = _commonData.ToDate1(requestGetAllExecuteHistory.Date);
                DateTime start = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {requestGetAllExecuteHistory.StartTime}"));
                DateTime end = _commonData.ToDate1(date.ToString($"yyyy-MM-dd {requestGetAllExecuteHistory.EndTime}"));
                long startDate = _commonData.ConvertDateTimeToTimeSpan(start);

                long endDate;
                if (string.IsNullOrEmpty(requestGetAllExecuteHistory.EndTime))
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59));
                }
                else
                {
                    endDate = _commonData.ConvertDateTimeToTimeSpan(end);
                }

                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, start);
                Regex dbPattern = new(@"history_(\d{8}_\d{6})\.db$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db") : Array.Empty<string>();

                if (fileDB.Length == 0)
                {
                    fileDB = UnZipFileDB(requestGetAllExecuteHistory.TenantId ?? "", start);
                }

                var dbFiles = fileDB
                    .Select(file => new
                    {
                        Path = file,
                        Match = dbPattern.Match(file),
                        IsDefault = Path.GetFileName(file).Equals("history.db", StringComparison.OrdinalIgnoreCase)
                    })
                    .Select(f => new
                    {
                        f.Path,
                        f.IsDefault,
                        Date = f.Match.Success ? DateTime.ParseExact(f.Match.Groups[1].Value, "yyyyMMdd_HHmmss", null) : DateTime.MinValue
                    })
                    .OrderByDescending(f => f.Date)
                    .ToList();

                var filteredFiles = dbFiles
                    .Where(f => f.Date >= start && f.Date <= end)
                    .ToList();

                // Nếu không có file nào hoặc file nhỏ hơn start, lấy thêm 1 file tiếp theo
                if (!filteredFiles.Any() || dbFiles.Any(f => f.Date < start))
                {
                    var extraFile = dbFiles.FirstOrDefault(f => f.Date < start);
                    if (extraFile != null)
                    {
                        filteredFiles.Add(extraFile);
                    }
                }

                int pageSize = requestGetAllExecuteHistory.Take ?? 0;
                int skipCount = requestGetAllExecuteHistory.Skip ?? 0;
                var results = new List<ExecuteQuerySQLHistory>();
                int totalRecords = 0;
                foreach (var dbPath in filteredFiles)
                {
                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var countQuery = dbContextDaily.Context.H_SQLExecuteHistories
                            .Where(ptr => ptr.CreatedDate >= startDate && ptr.CreatedDate <= endDate
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WorkflowName) || (ptr.WfName != null && ptr.WfName.Contains(requestGetAllExecuteHistory.WorkflowName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Address) || (ptr.Address != null && ptr.Address.Contains(requestGetAllExecuteHistory.Address)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Catalog) || (ptr.Catalog != null && ptr.Catalog.Contains(requestGetAllExecuteHistory.Catalog)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.StepName) || (ptr.StepName != null && ptr.StepName.Contains(requestGetAllExecuteHistory.StepName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.StepId) || (ptr.StepId != null && ptr.StepId.Contains(requestGetAllExecuteHistory.StepId)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfId) || ptr.WfId == requestGetAllExecuteHistory.WfId)
                                && (requestGetAllExecuteHistory.Status == null || !statusList.Contains(requestGetAllExecuteHistory.Status.ToString())|| (ptr.Status != null && ptr.Status == requestGetAllExecuteHistory.Status.ToString()))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.SiteRun) || (ptr.SiteRun != null && ptr.SiteRun.Contains(requestGetAllExecuteHistory.SiteRun))))
                            .OrderByDescending(ptr => ptr.CreatedDate);

                        totalRecords += countQuery.Count();
                    }
                }

                int currentCount = 0;
                foreach (var dbPath in filteredFiles)
                {
                    if (results.Count >= pageSize) break; // Đã đủ số lượng cần lấy

                    using (var dbContextDaily = _historyContextFactory.CreateContext(dbPath.Path))
                    {
                        var query = dbContextDaily.Context.H_SQLExecuteHistories
                            .Where(ptr => ptr.CreatedDate >= startDate && ptr.CreatedDate <= endDate
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WorkflowName) || (ptr.WfName != null && ptr.WfName.Contains(requestGetAllExecuteHistory.WorkflowName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Address) || (ptr.Address != null && ptr.Address.Contains(requestGetAllExecuteHistory.Address)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.Catalog) || (ptr.Catalog != null && ptr.Catalog.Contains(requestGetAllExecuteHistory.Catalog)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.StepName) || (ptr.StepName != null && ptr.StepName.Contains(requestGetAllExecuteHistory.StepName)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.StepId) || (ptr.StepId != null && ptr.StepId.Contains(requestGetAllExecuteHistory.StepId)))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.WfId) || ptr.WfId == requestGetAllExecuteHistory.WfId)
                                && (requestGetAllExecuteHistory.Status == null || !statusList.Contains(requestGetAllExecuteHistory.Status.ToString()) || (ptr.Status != null && ptr.Status == requestGetAllExecuteHistory.Status.ToString()))
                                && (string.IsNullOrEmpty(requestGetAllExecuteHistory.SiteRun) || (ptr.SiteRun != null && ptr.SiteRun.Contains(requestGetAllExecuteHistory.SiteRun))))
                            .OrderByDescending(ptr => ptr.CreatedDate);

                        int remainingSkip = Math.Max(0, skipCount - currentCount);
                        var partialResult = query
                            .Skip(remainingSkip)
                            .Take(pageSize - results.Count)
                            .Select(ptr => new ExecuteQuerySQLHistory
                            {
                                KeyExecuteRunSQLThirdParty = ptr.KeyExecuteRunQuerySQL,
                                Reason = ptr.Reason,
                                SiteRun = ptr.SiteRun,
                                Status = ptr.Status == "Complete" ? WorkflowExecuteStatus.Complete :
                                    ptr.Status == "Running" ? WorkflowExecuteStatus.Running :
                                    WorkflowExecuteStatus.Fail,
                                TimeEnd = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate),
                                TimeStart = _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                TimeInterval = _commonData.ConvertTimeSpanToDateTime(ptr.ModificationDate) - _commonData.ConvertTimeSpanToDateTime(ptr.CreatedDate),
                                WorkflowId = ptr.WfId,
                                WorkflowName = ptr.WfName,
                                Address = ptr.Address,
                                Catalog = ptr.Catalog,
                                Query = ptr.Query,
                                RecordCount = ptr.RecordCount,
                                StepId = ptr.StepId,
                                StepName = ptr.StepName
                            })
                            .ToList();

                        currentCount += query.Count();

                        if (partialResult.Count > 0)
                        {
                            results.AddRange(partialResult);
                        }
                    }
                }

                return new ResponseGetAllExecuteQuerySQLHistory
                {
                    Data = results,
                    TotalReCords = totalRecords
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllQuerySQLExecuteHistories: {ex.Message}");
                throw;
            }
        }

        public string[] UnZipFileDB(string tenantId, DateTime start)
        {
            try
            {
                var fileDB = Array.Empty<string>();
                string extractPathDay = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDayUnZip, tenantId);
                string extractPathMonth = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogMonthUnZip, tenantId);
                _commonData.CreateFolder1(extractPathDay);
                _commonData.CreateFolder1(extractPathMonth);
                string pathDay = _createDB.GetFilePath(extractPathDay, start);
                string pathMonth = _createDB.GetFilePath(extractPathMonth, start);

                var dbFilesDayUnZip = Directory.Exists(pathDay) ? Directory.GetFiles(pathDay, "history*.db") : Array.Empty<string>();
                var dbFilesMonthZip = Directory.Exists(pathMonth) ? Directory.GetFiles(Path.Combine(pathMonth, $"BackupHistory_{start.ToString("yyyyMMdd")}"), "history*.db") : Array.Empty<string>();
                if (dbFilesDayUnZip.Length > 0)
                {
                    fileDB = dbFilesDayUnZip;
                }
                else if (dbFilesMonthZip.Length > 0)
                {
                    fileDB = dbFilesMonthZip;
                }
                else
                {
                    string pathDBZip = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogDay, tenantId);
                    string destinationZipPath = Path.Combine(Directory.GetCurrentDirectory(), DataPath.PathBackUpDBLogMonth, tenantId);
                    var zipFileDays = Directory.Exists(pathDBZip) ? Directory.GetFiles(pathDBZip, "*.zip")
                            .Select(file => new FileInfo(file))
                            .Where(ptr => ptr.Name.Contains(start.ToString("yyyyMMdd")))
                            .FirstOrDefault() : null;
                    if (zipFileDays != null &&
                        zipFileDays.Length > 0)
                    {
                        string zipPath = zipFileDays.FullName;
                        var pathZipDay = ExtractZip(start.ToString("yyyyMMdd"), zipPath, extractPathDay);
                        fileDB = Directory.Exists(pathZipDay) ? Directory.GetFiles(pathZipDay, "history*.db") : Array.Empty<string>();
                    }
                    else
                    {
                        var zipFileMonths = Directory.Exists(destinationZipPath) ? Directory.GetFiles(destinationZipPath, "*.zip")
                        .Select(file => new FileInfo(file))
                        .Where(ptr => ptr.Name.Contains(start.ToString("yyyyMMdd")))
                        .FirstOrDefault() : null;
                        if (zipFileMonths != null &&
                            zipFileMonths.Length > 0)
                        {
                            string zipPath = zipFileMonths.FullName;
                            var pathZipMonth = ExtractZip(start.ToString("yyyyMMdd"), zipPath, extractPathMonth);
                            fileDB = Directory.Exists(pathZipMonth) ? Directory.GetFiles(pathZipMonth, "history*.db") : Array.Empty<string>();
                        }
                        else
                        {
                            return Array.Empty<string>();
                        }
                    }
                }
                return fileDB;
            }
            catch (Exception ex)
            {
                Log.Error($"UnZipFileDB: {ex.Message}");
                throw;
            }
        }

        public string ExtractZip(string nameDate, string zipPath, string destinationPath, int depth = 0)
        {
            const int THRESHOLD_ENTRIES = 10000;
            const long THRESHOLD_SIZE = 1_000_000_000;
            const double THRESHOLD_RATIO = 100;
            const int MAX_DEPTH = 100;

            if (depth > MAX_DEPTH)
            {
                Log.Error("Vượt quá giới hạn độ sâu cho ZIP lồng nhau!");
                return "";
            }

            long totalSizeArchive = 0;
            int totalEntryArchive = 0;
            string lastExtractedFilePath = "";

            try
            {
                using var archive = ZipFile.OpenRead(zipPath);

                foreach (var entry in archive.Entries)
                {
                    totalEntryArchive++;
                    if (totalEntryArchive > THRESHOLD_ENTRIES)
                    {
                        Log.Error($"Quá nhiều mục trong kho lưu trữ! Giới hạn tối đa: {THRESHOLD_ENTRIES}");
                        return "";
                    }

                    string fullPath = GetSafeFullPath(destinationPath, entry.FullName);
                    if (!fullPath.StartsWith(destinationPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Warning($"Blocked path traversal attempt: {entry.FullName}");
                        continue;
                    }

                    // Tạo thư mục nếu entry là folder
                    if (entry.FullName.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
                    {
                        _commonData.CreateFolder1(fullPath);
                        continue;
                    }

                    if (entry.CompressedLength > THRESHOLD_SIZE || entry.Length > THRESHOLD_SIZE)
                    {
                        Log.Warning($"File {entry.FullName} vượt quá giới hạn kích thước!");
                        continue;
                    }

                    long totalSizeEntry = 0;
                    using (var entryStream = entry.Open())
                    {
                        byte[] buffer = new byte[4096];
                        int numBytesRead;
                        while ((numBytesRead = entryStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            totalSizeEntry += numBytesRead;
                            totalSizeArchive += numBytesRead;

                            if (totalSizeArchive > THRESHOLD_SIZE)
                            {
                                Log.Error($"Tổng kích thước giải nén vượt quá {THRESHOLD_SIZE} byte!");
                                return "";
                            }
                        }
                    }

                    if (entry.CompressedLength > 0)
                    {
                        double compressionRatio = (double)totalSizeEntry / entry.CompressedLength;
                        if (compressionRatio > THRESHOLD_RATIO)
                        {
                            Log.Error("Tệp ZIP lồng nhau quá nhiều, có thể là Zip Bomb!");
                            return "";
                        }
                    }

                    if (entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!entry.Name.Contains(nameDate)) continue;

                        string subExtractPath = Path.Combine(destinationPath, Path.GetFileNameWithoutExtension(entry.FullName));
                        _commonData.CreateFolder1(subExtractPath);
                        string tempZipPath = Path.Combine(destinationPath, entry.Name);

                        try
                        {
                            using (var entryStream = entry.Open())
                            using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                entryStream.CopyTo(fs);
                            }

                            lastExtractedFilePath = ExtractZip(nameDate, tempZipPath, subExtractPath, depth + 1);
                            File.Delete(tempZipPath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Lỗi giải nén tệp ZIP con {entry.Name}: {ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);

                            using (var entryStream = entry.Open())
                            using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                entryStream.CopyTo(fs);
                            }
                            File.SetLastWriteTime(fullPath, DateTime.Now);
                            lastExtractedFilePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Lỗi khi giải nén {entry.FullName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                Log.Error($"Lỗi quyền truy cập khi đọc ZIP: {uae.Message}");
            }

            return lastExtractedFilePath;
        }

        private string GetSafeFullPath(string basePath, string entryFullName)
        {
            string fullPath = Path.GetFullPath(Path.Combine(basePath, entryFullName));
            if (!fullPath.StartsWith(Path.GetFullPath(basePath), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Đường dẫn không an toàn: {entryFullName}");
            }
            return fullPath;
        }
        public List<ResponseExecuteHistoryDay> GetApiThirdPartyExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            try
            {
                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request.TenantId);
                _commonData.CreateFolder1(pathNow);
                var summaryDict = Enumerable.Range(0, 24)
                       .Select(hour => new ResponseExecuteHistoryDay
                       {
                           Hour = hour,
                           CountSuccess = 0,
                           CountFail = 0
                       })
                       .ToDictionary(s => s.Hour);
                if (!Directory.Exists(pathNow))
                {
                    return summaryDict.Values.ToList();
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();
                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                                .Where(ptr => (ptr.Status == ExecuteRunApiThirdPartyStatus.Complete.ToString()
                                        || ptr.Status == ExecuteRunApiThirdPartyStatus.Fail.ToString())
                                        && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                                .GroupBy(ptr => new
                                {
                                    Hour = ptr.Hour,
                                    Status = ptr.Status
                                })
                                .Select(group => new
                                {
                                    Hour = group.Key.Hour,
                                    Status = group.Key.Status,
                                    Count = group.Count()
                                }).ToList();
                            foreach (var entry in query)
                            {
                                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                                {
                                    if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Complete.ToString()))
                                    {
                                        summary.CountSuccess += entry.Count;
                                    }
                                    else if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Fail.ToString()))
                                    {
                                        summary.CountFail += entry.Count;
                                    }
                                }
                            }
                        }
                    }
                }

                return summaryDict.Values.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ResponseExecuteHistoryDay> GetQuerySQLExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            try
            {

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request.TenantId);

                _commonData.CreateFolder1(pathNow);
                var summaryDict = Enumerable.Range(0, 24)
                       .Select(hour => new ResponseExecuteHistoryDay
                       {
                           Hour = hour,
                           CountSuccess = 0,
                           CountFail = 0
                       })
                       .ToDictionary(s => s.Hour);
                if (!Directory.Exists(pathNow))
                {
                    return summaryDict.Values.ToList();
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();
                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_SQLExecuteHistories
                                .Where(ptr => (ptr.Status == ExecuteRunApiThirdPartyStatus.Complete.ToString()
                                        || ptr.Status == ExecuteRunApiThirdPartyStatus.Fail.ToString())
                                        && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                                .GroupBy(ptr => new
                                {
                                    Hour = ptr.Hour,
                                    Status = ptr.Status
                                })
                                .Select(group => new
                                {
                                    Hour = group.Key.Hour,
                                    Status = group.Key.Status,
                                    Count = group.Count()
                                }).ToList();
                            foreach (var entry in query)
                            {
                                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                                {
                                    if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Complete.ToString()))
                                    {
                                        summary.CountSuccess += entry.Count;
                                    }
                                    else if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Fail.ToString()))
                                    {
                                        summary.CountFail += entry.Count;
                                    }
                                }
                            }
                        }
                    }
                }
                return summaryDict.Values.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ResponseExecuteHistoryDay> GetApiThirdPartyExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            var summaryDict = Enumerable.Range(0, 24)
                    .ToDictionary(hour => hour, hour => new ResponseExecuteHistoryDay
                    {
                        Hour = hour,
                        CountSuccess = 0,
                        CountFail = 0
                    });
            var selectedDate = !string.IsNullOrEmpty(request.SelectDate)
                            ? _commonData.ToDate1(request.SelectDate).ToString("yyyy-MM-dd")
                            : null;

            var baseQuery = tenantContext.Context.SQLite_HistoryDays
                .Where(ptr =>
                    ptr.Type == Database.Tenant.Tables.TypeHistory.API &&
                    (string.IsNullOrEmpty(selectedDate) || ptr.Date == selectedDate) &&
                    (string.IsNullOrEmpty(request.Site) || ptr.Site == request.Site));

            var query = string.IsNullOrEmpty(request.Site)
                ? baseQuery
                    .GroupBy(ptr => ptr.Hour)
                    .Select(group => new
                    {
                        Hour = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                    .ToList()
                : baseQuery
                    .Select(ptr => new
                    {
                        Hour = ptr.Hour,
                        CountSuccess = ptr.CountSuccess,
                        CountFail = ptr.CountFail
                    })
                    .ToList();

            foreach (var entry in query)
            {
                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }

            return summaryDict.Values.ToList();
        }
        public List<ResponseExecuteHistoryDay> GetWorkflowExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            try
            {

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request.TenantId);

                _commonData.CreateFolder1(pathNow);
                var summaryDict = Enumerable.Range(0, 24)
                     .Select(hour => new ResponseExecuteHistoryDay
                     {
                         Hour = hour,
                         CountSuccess = 0,
                         CountFail = 0
                     })
                     .ToDictionary(s => s.Hour);
                if (!Directory.Exists(pathNow))
                {
                    return summaryDict.Values.ToList();
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);

                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();

                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_WorkflowExecuteHistories
                                .Where(ptr => (ptr.Status == WorkflowExecuteStatus.Complete.ToString()
                                        || ptr.Status == WorkflowExecuteStatus.Fail.ToString())
                                        && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                                .GroupBy(ptr => new
                                {
                                    Hour = ptr.Hour,
                                    Status = ptr.Status
                                })
                                .Select(group => new
                                {
                                    Hour = group.Key.Hour,
                                    Status = group.Key.Status,
                                    Count = group.Count()
                                }).ToList();
                            foreach (var entry in query)
                            {
                                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                                {

                                    if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Complete.ToString()))
                                    {
                                        summary.CountSuccess += entry.Count;
                                    }
                                    else if (entry.Status.Equals(ExecuteRunApiThirdPartyStatus.Fail.ToString()))
                                    {
                                        summary.CountFail += entry.Count;
                                    }

                                }
                            }
                        }
                    }
                }

                return summaryDict.Values.ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ResponseExecuteHistoryDay> GetWorkflowExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            var summaryDict = Enumerable.Range(0, 24)
                    .ToDictionary(hour => hour, hour => new ResponseExecuteHistoryDay
                    {
                        Hour = hour,
                        CountSuccess = 0,
                        CountFail = 0
                    });
            var selectedDate = !string.IsNullOrEmpty(request.SelectDate)
                            ? _commonData.ToDate1(request.SelectDate).ToString("yyyy-MM-dd")
                            : null;
            var baseQuery = tenantContext.Context.SQLite_HistoryDays
           .Where(ptr =>
               ptr.Type == Database.Tenant.Tables.TypeHistory.WF &&
               (string.IsNullOrEmpty(selectedDate) || ptr.Date == selectedDate) &&
               (string.IsNullOrEmpty(request.Site) || ptr.Site == request.Site));

            var query = string.IsNullOrEmpty(request.Site)
                ? baseQuery
                    .GroupBy(ptr => ptr.Hour)
                    .Select(group => new
                    {
                        Hour = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                    .ToList()
                : baseQuery
                    .Select(ptr => new
                    {
                        Hour = ptr.Hour,
                        CountSuccess = ptr.CountSuccess,
                        CountFail = ptr.CountFail
                    })
                    .ToList();

            foreach (var entry in query)
            {
                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }

            return summaryDict.Values.ToList();
        }
        public List<ResponseExecuteHistoryDay> GetQuerySQLExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            var summaryDict = Enumerable.Range(0, 24)
                    .ToDictionary(hour => hour, hour => new ResponseExecuteHistoryDay
                    {
                        Hour = hour,
                        CountSuccess = 0,
                        CountFail = 0
                    });
            var selectedDate = !string.IsNullOrEmpty(request.SelectDate)
                            ? _commonData.ToDate1(request.SelectDate).ToString("yyyy-MM-dd")
                            : null;
            var baseQuery = tenantContext.Context.SQLite_HistoryDays
           .Where(ptr =>
               ptr.Type == Database.Tenant.Tables.TypeHistory.SQL &&
               (string.IsNullOrEmpty(selectedDate) || ptr.Date == selectedDate) &&
               (string.IsNullOrEmpty(request.Site) || ptr.Site == request.Site));

            var query = string.IsNullOrEmpty(request.Site)
                ? baseQuery
                    .GroupBy(ptr => ptr.Hour)
                    .Select(group => new
                    {
                        Hour = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                    .ToList()
                : baseQuery
                    .Select(ptr => new
                    {
                        Hour = ptr.Hour,
                        CountSuccess = ptr.CountSuccess,
                        CountFail = ptr.CountFail
                    })
                    .ToList();

            foreach (var entry in query)
            {
                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }

            return summaryDict.Values.ToList();
        }
        public ResponseExecuteHistoryMonth? GetApiThirdPartyExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            try
            {
                DateTime dateTime = _commonData.ToDate1(request.SelectDate);
                if (dateTime.ToString("MM") != DateTime.Now.ToString("MM"))
                {
                    return null;
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request?.TenantId ?? "");
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseExecuteHistoryMonth()
                    {
                        CountFail = 0,
                        CountSuccess = 0,
                        Date = DateTime.Now.ToString("dd")
                    };
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();
                int countSuccess = 0;
                int countFail = 0;
                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_ApiThirdPartyExecuteHistories
                            .Where(ptr =>
                                new[] {
                                    Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                                    Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Fail.ToString()
                                }.Contains(ptr.Status)
                                && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                            .GroupBy(ptr => ptr.Status)
                            .Select(g => new
                            {
                                Status = g.Key,
                                Count = g.Count()
                            })
                            .ToList();


                            countSuccess += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Complete.ToString())?.Count ?? 0;
                            countFail += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Fail.ToString())?.Count ?? 0;
                        }
                    }
                }

                return new ResponseExecuteHistoryMonth()
                {
                    CountFail = countFail,
                    CountSuccess = countSuccess,
                    Date = DateTime.Now.ToString("dd")
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public ResponseExecuteHistoryMonth? GetQuerySQLExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            try
            {
                DateTime dateTime = _commonData.ToDate1(request.SelectDate);
                if (dateTime.ToString("MM") != DateTime.Now.ToString("MM"))
                {
                    return null;
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request.TenantId);
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseExecuteHistoryMonth()
                    {
                        CountFail = 0,
                        CountSuccess = 0,
                        Date = DateTime.Now.ToString("dd")
                    };
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();
                int countSuccess = 0;
                int countFail = 0;
                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_SQLExecuteHistories
                               .Where(ptr =>
                                   new[] {
                                        Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                                        Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Fail.ToString()
                                   }.Contains(ptr.Status)
                                   && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                               .GroupBy(ptr => ptr.Status)
                               .Select(g => new
                               {
                                   Status = g.Key,
                                   Count = g.Count()
                               })
                               .ToList();

                            countSuccess += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Complete.ToString())?.Count ?? 0;
                            countFail += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Fail.ToString())?.Count ?? 0;
                        }
                    }
                }

                return new ResponseExecuteHistoryMonth()
                {
                    CountFail = countFail,
                    CountSuccess = countSuccess,
                    Date = DateTime.Now.ToString("dd")
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ResponseExecuteHistoryMonth> GetApiThirdPartyExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            DateTime selectDate = DateTime.Parse(request.SelectDate, System.Globalization.CultureInfo.InvariantCulture);

            var startOfMonth = new DateTime(selectDate.Year, selectDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var summaryList = Enumerable.Range(1, DateTime.DaysInMonth(selectDate.Year, selectDate.Month))
                .Select(day => new ResponseExecuteHistoryMonth()
                {
                    Date = day.ToString("00"),
                    CountSuccess = 0,
                    CountFail = 0
                })
                .ToList();

            var result = string.IsNullOrEmpty(request.Site)
               ? tenantContext.Context.SQLite_HistoryMonths
                   .Where(ptr =>
                       ptr.Type == Database.Tenant.Tables.TypeHistory.API &&
                       ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                   .ToList()
               : tenantContext.Context.SQLite_HistoryMonths
                   .Where(ptr =>
                       ptr.Type == Database.Tenant.Tables.TypeHistory.API &&
                       ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth &&
                       ptr.Site == request.Site)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                   .ToList();



            foreach (var entry in result)
            {
                var summary = summaryList.Find(s => s.Date == entry.Date);

                if (summary != null)
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }

            return summaryList;
        }
        public List<ResponseExecuteHistoryMonth> GetQuerySQLExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            DateTime selectDate = DateTime.Parse(request.SelectDate, System.Globalization.CultureInfo.InvariantCulture);

            var startOfMonth = new DateTime(selectDate.Year, selectDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var summaryList = Enumerable.Range(1, DateTime.DaysInMonth(selectDate.Year, selectDate.Month))
                .Select(day => new ResponseExecuteHistoryMonth()
                {
                    Date = day.ToString("00"),
                    CountSuccess = 0,
                    CountFail = 0
                })
                .ToList();

            var result = string.IsNullOrEmpty(request.Site)
               ? tenantContext.Context.SQLite_HistoryMonths
                   .Where(ptr =>
                       ptr.Type == Database.Tenant.Tables.TypeHistory.SQL &&
                       ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                   .ToList()
               : tenantContext.Context.SQLite_HistoryMonths
                   .Where(ptr =>
                       ptr.Type == Database.Tenant.Tables.TypeHistory.SQL &&
                       ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth &&
                       ptr.Site == request.Site)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                   .ToList();

            foreach (var entry in result)
            {
                var summary = summaryList.Find(s => s.Date == entry.Date);

                if (summary != null)
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }

            return summaryList;
        }
        public ResponseExecuteHistoryMonth? GetWorkflowExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            try
            {
                DateTime dateTime = _commonData.ToDate1(request.SelectDate);
                if (dateTime.ToString("MM") != DateTime.Now.ToString("MM"))
                {
                    return null;
                }

                string pathNow = Path.Combine(Directory.GetCurrentDirectory(), DataPath.DataBaseLogIBox, request.TenantId);
                _commonData.CreateFolder1(pathNow);
                if (!Directory.Exists(pathNow))
                {
                    return new ResponseExecuteHistoryMonth()
                    {
                        CountFail = 0,
                        CountSuccess = 0,
                        Date = DateTime.Now.ToString("dd")
                    };
                }

                string fileDBChatBotNow1 = "";
                fileDBChatBotNow1 = _createDB.GetFilePath(pathNow, DateTime.Now);
                _commonData.CreateFolder1(fileDBChatBotNow1);
                var fileDB = Directory.Exists(fileDBChatBotNow1) ? Directory.GetFiles(fileDBChatBotNow1, "history*.db").ToList() : new List<string>();
                int countSuccess = 0;
                int countFail = 0;
                foreach (var filePath in fileDB)
                {
                    if (File.Exists(filePath))
                    {
                        using (var dbContextDaily = _historyContextFactory.CreateContext(filePath))
                        {
                            var query = dbContextDaily.Context.H_WorkflowExecuteHistories
                              .Where(ptr =>
                                  new[] {
                                            Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Complete.ToString(),
                                            Database.SQLiteDBChatDay.TableDBHistory.ExecuteRunApiThirdPartyStatus.Fail.ToString()
                                  }.Contains(ptr.Status)
                                  && (string.IsNullOrEmpty(request.Site) || ptr.SiteRun == request.Site))
                              .GroupBy(ptr => ptr.Status)
                              .Select(g => new
                              {
                                  Status = g.Key,
                                  Count = g.Count()
                              })
                              .ToList();

                            countSuccess += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.WorkflowExecuteStatus.Complete.ToString())?.Count ?? 0;
                            countFail += query.FirstOrDefault(x => x.Status == Database.SQLiteDBChatDay.TableDBHistory.WorkflowExecuteStatus.Fail.ToString())?.Count ?? 0;
                        }
                    }
                }

                return new ResponseExecuteHistoryMonth()
                {
                    CountFail = countFail,
                    CountSuccess = countSuccess,
                    Date = DateTime.Now.ToString("dd")
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        public List<ResponseExecuteHistoryMonth> GetWorkflowExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantContext = _tenantContext.GetTenantContext(request.TenantId);
            DateTime selectDate = DateTime.Parse(request.SelectDate, System.Globalization.CultureInfo.InvariantCulture);

            var startOfMonth = new DateTime(selectDate.Year, selectDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var summaryList = Enumerable.Range(1, DateTime.DaysInMonth(selectDate.Year, selectDate.Month))
                .Select(day => new ResponseExecuteHistoryMonth()
                {
                    Date = day.ToString("00"),
                    CountSuccess = 0,
                    CountFail = 0
                })
                .ToList();



            var result = string.IsNullOrEmpty(request.Site)
                ? tenantContext.Context.SQLite_HistoryMonths
                    .Where(ptr =>
                        ptr.Type == Database.Tenant.Tables.TypeHistory.WF &&
                        ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                    .ToList()
                : tenantContext.Context.SQLite_HistoryMonths
                    .Where(ptr =>
                        ptr.Type == Database.Tenant.Tables.TypeHistory.WF &&
                        ptr.DateReal >= startOfMonth && ptr.DateReal <= endOfMonth &&
                        ptr.Site == request.Site)
                    .GroupBy(ptr => ptr.Date.Substring(ptr.Date.Length - 2))
                    .Select(group => new ResponseExecuteHistoryMonth
                    {
                        Date = group.Key,
                        CountSuccess = group.Sum(ptr => ptr.CountSuccess),
                        CountFail = group.Sum(ptr => ptr.CountFail)
                    })
                    .ToList();



            foreach (var entry in result)
            {
                var summary = summaryList.Find(s => s.Date == entry.Date);

                if (summary != null)
                {
                    summary.CountSuccess = entry.CountSuccess;
                    summary.CountFail = entry.CountFail;
                }
            }
            return summaryList;
        }
        public T? CallGetDataHistory<T>(object body, string url, string author, string tenantId)
        {
            try
            {
                var result = _restAPI.Send(new RestAPIRequest()
                {
                    Body = JsonConvert.SerializeObject(body),
                    Headers = new List<RestAPIHeader>
                        {
                            new RestAPIHeader ()
                            {
                                Label = "Content-Type",
                                Value = "application/json"
                            },
                             new RestAPIHeader ()
                            {
                                Label = "Authorization",
                                Value = author
                            }
                        },
                    Method = "POST",
                    Timeout = 30,
                    Url = url
                }, tenantId);

                if (string.IsNullOrEmpty(result?.Result))
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<T>(result.Result);
            }
            catch (Exception ex)
            {
                Log.Error($"CallWFGetDataHistory: {ex.Message}");
                return default;
            }
        }
    }
}