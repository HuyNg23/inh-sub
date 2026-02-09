using Azure;
using DocumentFormat.OpenXml.Wordprocessing;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.ServiceIB;
using IBox.Database.Root;
using IBox.Database.Tenant.ServicesManager;
using IBox.History.DB.WorkflowAndApiThirdPartyHistory;
using IBox.Security;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Drawing.Printing;
using System.Security.Policy;
using System.Text;

namespace IBox.LogService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogServiceController : ControllerBase
    {
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IHandelWFAndAPI _handelWFAndAPI;
        private readonly ICommonData _commonData;
        private readonly IGetDataLogStream _dataLogStream;

        public LogServiceController(Common.Objects.IConfiguration configuration, IHandelWFAndAPI handelWFAndAPI, ICommonData commonData, IGetDataLogStream dataLogStream)
        {
            _configuration = configuration;
            _handelWFAndAPI = handelWFAndAPI;
            _commonData = commonData;
            _dataLogStream = dataLogStream;
        }
        [HttpGet("LoadGetDataLog")]
        public IActionResult LoadGetDataLog()
        {
            try
            {
                _dataLogStream.GetDataLog();
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"LoadGetDataLog: {ex.Message}");
                throw;
            }
        }

        [HttpGet("SetDataGetLog")]
        public IActionResult SetDataGetLog(string input)
        {
            try
            {
                string logStatusFilePath = Path.Combine(Directory.GetCurrentDirectory(), "status.log");
                System.IO.File.WriteAllText(logStatusFilePath, input);
                IBGlobalConfig.IsRunLog = input;
                return Ok();
            }
            catch (Exception ex)
            {
                Log.Error($"SetDataGetLog: {ex.Message}");
                throw;
            }
        }

        #region WF

        [HttpPost("GetWorkflowExecuteHistoryDay")]
        [IBoxAuthorization]
        [IBoxActionPermission("home-workflow-view", "history-workflow")]
        public ResponseForm<List<ResponseExecuteHistoryDay>> GetWorkflowExecuteHistoryDay(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }
            return new ResponseForm<List<ResponseExecuteHistoryDay>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                DateTime dateTime = _commonData.ToDate1(request.Body.SelectDate);

                var summaryDict = Enumerable.Range(0, 24)
                      .Select(hour => new ResponseExecuteHistoryDay
                      {
                          Hour = hour,
                          CountSuccess = 0,
                          CountFail = 0
                      })
                      .ToDictionary(s => s.Hour);

                if (dateTime.ToString("yyyy-MM-dd") != DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    var responseWFExecuteHistorySQLiteDaysCurrent = _handelWFAndAPI.GetWorkflowExecuteHistoryDaySQLServer(request.Body);
                    return responseWFExecuteHistorySQLiteDaysCurrent;
                }
                else
                {
                    var authorization = Request.Headers["Authorization"].ToString();
                    var responseAllWorkflowExecuteHistory = new List<ResponseExecuteHistoryDay>();
                    foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                    {
                        string urlLogService = string.Format(@"{0}{1}",
                              urlLog,
                              UrlServiceIBConfig.UrlGetWFStatusSDaySQLite
                            );
                        List<ResponseExecuteHistoryDay>? responseWFExecuteHistorySQLiteDays = _handelWFAndAPI.CallGetDataHistory<List<ResponseExecuteHistoryDay>>(request.Body, urlLogService, authorization, tenantID);

                        if (responseWFExecuteHistorySQLiteDays != null)
                        {
                            foreach (var responseExecuteHistoryDay in responseWFExecuteHistorySQLiteDays)
                            {
                                if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                                {
                                    summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                    summary.CountFail += responseExecuteHistoryDay.CountFail;
                                }
                            }
                        }
                    }

                    var responseWFExecuteHistorySQLiteDaysCurrent = _handelWFAndAPI.GetWorkflowExecuteHistoryDayToDaySQLite(request.Body);
                    if (responseWFExecuteHistorySQLiteDaysCurrent != null)
                    {
                        foreach (var responseExecuteHistoryDay in responseWFExecuteHistorySQLiteDaysCurrent)
                        {
                            if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                            {
                                summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                summary.CountFail += responseExecuteHistoryDay.CountFail;
                            }
                        }
                    }
                }

                return summaryDict.Values.ToList();
            });
        }

        [HttpPost("GetWorkflowExecuteHistoryDayDetails")]
        [IBoxAuthorization]
        public List<ResponseExecuteHistoryDay> GetWorkflowExecuteHistoryDayDetails(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            List<ResponseExecuteHistoryDay> historyDaySQLServers = new List<ResponseExecuteHistoryDay>();
            historyDaySQLServers = _handelWFAndAPI.GetWorkflowExecuteHistoryDayToDaySQLite(request);
            return historyDaySQLServers;
        }

        [HttpPost("GetWorkflowExecuteHistoryMonth")]
        [IBoxAuthorization]
        [IBoxActionPermission("home-workflow-view", "history-workflow")]
        public ResponseForm<List<ResponseExecuteHistoryMonth>> GetWorkflowExecuteHistoryMonth(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResponseExecuteHistoryMonth>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();
                //Lấy data từ history sql server
                var historyMonthSQLServers = _handelWFAndAPI.GetWorkflowExecuteHistoryMonthSQLServer(request.Body);
                //lấy data theo ngày từ sqlite
                foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                {
                    string urlLogService = string.Format(@"{0}{1}",
                          urlLog,
                          UrlServiceIBConfig.UrlGetWFStatusMonthSQLite
                        );
                    ResponseExecuteHistoryMonth? responseWFExecuteHistorySQLiteMonth = _handelWFAndAPI.CallGetDataHistory<ResponseExecuteHistoryMonth>(request.Body, urlLogService, authorization, tenantID);

                    if (responseWFExecuteHistorySQLiteMonth != null)
                    {
                        var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == responseWFExecuteHistorySQLiteMonth.Date);
                        if (historyMonthSQLServer != null)
                        {
                            historyMonthSQLServer.CountSuccess += responseWFExecuteHistorySQLiteMonth.CountSuccess;
                            historyMonthSQLServer.CountFail += responseWFExecuteHistorySQLiteMonth.CountFail;
                        }
                    }
                }

                //Call api đến server còn lại lấy data và gộp data hiện tại
                var historyMonthSQLite = _handelWFAndAPI.GetWorkflowExecuteHistoryMonthToDaySQLite(request.Body);
                if (historyMonthSQLite != null)
                {
                    var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == historyMonthSQLite.Date);
                    if (historyMonthSQLServer != null)
                    {
                        historyMonthSQLServer.CountSuccess += historyMonthSQLite.CountSuccess;
                        historyMonthSQLServer.CountFail += historyMonthSQLite.CountFail;
                    }
                }

                return historyMonthSQLServers;
            });
        }

        [HttpPost("GetWorkflowExecuteHistoryMonthSQLiteDetails")]
        [IBoxAuthorization]
        public ResponseExecuteHistoryMonth GetWorkflowExecuteHistoryMonthSQLiteDetails(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            var historyMonthSQLite = _handelWFAndAPI.GetWorkflowExecuteHistoryMonthToDaySQLite(request);
            return historyMonthSQLite;
        }

        #endregion WF

        #region QuerySQL

        [HttpPost("GetQuerySQLExecuteHistoryDay")]
        [IBoxAuthorization]
        public ResponseForm<List<ResponseExecuteHistoryDay>> GetQuerySQLExecuteHistoryDay(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResponseExecuteHistoryDay>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                DateTime dateTime = _commonData.ToDate1(request.Body.SelectDate);
                List<ResponseExecuteHistoryDay> historyAPIDaySQLServers = new List<ResponseExecuteHistoryDay>();

                var summaryDict = Enumerable.Range(0, 24)
                      .Select(hour => new ResponseExecuteHistoryDay
                      {
                          Hour = hour,
                          CountSuccess = 0,
                          CountFail = 0
                      })
                      .ToDictionary(s => s.Hour);

                if (dateTime.ToString("yyyy-MM-dd") != DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    //Lấy data từ history sql server
                    historyAPIDaySQLServers = _handelWFAndAPI.GetQuerySQLExecuteHistoryDaySQLServer(request.Body);
                    return historyAPIDaySQLServers;
                }
                else
                {
                    var authorization = Request.Headers["Authorization"].ToString();
                    var responseAllWorkflowExecuteHistory = new List<ResponseExecuteHistoryDay>();
                    foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                    {
                        string urlLogService = string.Format(@"{0}{1}",
                              urlLog,
                              UrlServiceIBConfig.UrlGetQuerySQLStatusSDaySQLite
                            );
                        List<ResponseExecuteHistoryDay>? responseAPIExecuteHistorySQLiteDays = _handelWFAndAPI.CallGetDataHistory<List<ResponseExecuteHistoryDay>>(request.Body, urlLogService, authorization, tenantID);

                        if (responseAPIExecuteHistorySQLiteDays != null)
                        {
                            foreach (var responseExecuteHistoryDay in responseAPIExecuteHistorySQLiteDays)
                            {
                                if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                                {
                                    summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                    summary.CountFail += responseExecuteHistoryDay.CountFail;
                                }
                            }
                        }
                    }

                    var responseAPIExecuteHistorySQLiteDaysCurrent = _handelWFAndAPI.GetQuerySQLExecuteHistoryDayToDaySQLite(request.Body);
                    if (responseAPIExecuteHistorySQLiteDaysCurrent != null)
                    {
                        foreach (var responseExecuteHistoryDay in responseAPIExecuteHistorySQLiteDaysCurrent)
                        {
                            if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                            {
                                summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                summary.CountFail += responseExecuteHistoryDay.CountFail;
                            }
                        }
                    }
                }

                return summaryDict.Values.ToList();
            });
        }

        [HttpPost("GetQuerySQLExecuteHistoryDaySQLite")]
        [IBoxAuthorization]
        public List<ResponseExecuteHistoryDay> GetQuerySQLExecuteHistoryDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            DateTime dateTime = _commonData.ToDate1(request.SelectDate);
            List<ResponseExecuteHistoryDay> historyAPIDaySQLServers = new List<ResponseExecuteHistoryDay>();
            historyAPIDaySQLServers = _handelWFAndAPI.GetQuerySQLExecuteHistoryDayToDaySQLite(request);
            return historyAPIDaySQLServers;
        }

        [HttpPost("GetQuerySQLExecuteHistoryMonth")]
        [IBoxAuthorization]
        public ResponseForm<List<ResponseExecuteHistoryMonth>> GetQuerySQLExecuteHistoryMonth(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResponseExecuteHistoryMonth>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();
                var historyMonthSQLServers = _handelWFAndAPI.GetQuerySQLExecuteHistoryMonthSQLServer(request.Body);
                foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                {
                    string urlLogService = string.Format(@"{0}{1}",
                          urlLog,
                          UrlServiceIBConfig.UrlGetQuerySQLStatusMonthSQLite
                        );
                    ResponseExecuteHistoryMonth? responseAPIExecuteHistorySQLiteMonth = _handelWFAndAPI.CallGetDataHistory<ResponseExecuteHistoryMonth>(request.Body, urlLogService, authorization, tenantID);

                    if (responseAPIExecuteHistorySQLiteMonth != null)
                    {
                        var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == responseAPIExecuteHistorySQLiteMonth.Date);
                        if (historyMonthSQLServer != null)
                        {
                            historyMonthSQLServer.CountSuccess += responseAPIExecuteHistorySQLiteMonth.CountSuccess;
                            historyMonthSQLServer.CountFail += responseAPIExecuteHistorySQLiteMonth.CountFail;
                        }
                    }
                }

                var historyMonthToDaySQLite = _handelWFAndAPI.GetQuerySQLExecuteHistoryMonthToDaySQLite(request.Body);
                if (historyMonthToDaySQLite != null)
                {
                    var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == historyMonthToDaySQLite.Date);
                    if (historyMonthSQLServer != null)
                    {
                        historyMonthSQLServer.CountSuccess += historyMonthToDaySQLite.CountSuccess;
                        historyMonthSQLServer.CountFail += historyMonthToDaySQLite.CountFail;
                    }
                }

                return historyMonthSQLServers;
            });
        }

        [HttpPost("GetQuerySQLExecuteHistoryMonthSQLiteDetails")]
        [IBoxAuthorization]
        public ResponseExecuteHistoryMonth GetQuerySQLExecuteHistoryMonthDetails(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            var historyMonthToDaySQLite = _handelWFAndAPI.GetQuerySQLExecuteHistoryMonthToDaySQLite(request);
            return historyMonthToDaySQLite;
        }

        #endregion QuerySQL

        #region API History

        [HttpPost("GetApiThirdPartyExecuteHistoryDay")]
        [IBoxAuthorization]
        public ResponseForm<List<ResponseExecuteHistoryDay>> GetApiThirdPartyExecuteHistoryDay(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResponseExecuteHistoryDay>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                DateTime dateTime = _commonData.ToDate1(request.Body.SelectDate);
                List<ResponseExecuteHistoryDay> historyAPIDaySQLServers = new List<ResponseExecuteHistoryDay>();

                var summaryDict = Enumerable.Range(0, 24)
                      .Select(hour => new ResponseExecuteHistoryDay
                      {
                          Hour = hour,
                          CountSuccess = 0,
                          CountFail = 0
                      })
                      .ToDictionary(s => s.Hour);

                if (dateTime.ToString("yyyy-MM-dd") != DateTime.Now.ToString("yyyy-MM-dd"))
                {
                    //Lấy data từ history sql server
                    historyAPIDaySQLServers = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryDaySQLServer(request.Body);
                    return historyAPIDaySQLServers;
                }
                else
                {
                    var authorization = Request.Headers["Authorization"].ToString();
                    var responseAllWorkflowExecuteHistory = new List<ResponseExecuteHistoryDay>();
                    foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                    {
                        string urlLogService = string.Format(@"{0}{1}",
                              urlLog,
                              UrlServiceIBConfig.UrlGetAPIStatusDaySQLite
                            );
                        List<ResponseExecuteHistoryDay>? responseAPIExecuteHistorySQLiteDays = _handelWFAndAPI.CallGetDataHistory<List<ResponseExecuteHistoryDay>>(request.Body, urlLogService, authorization, tenantID);

                        if (responseAPIExecuteHistorySQLiteDays != null)
                        {
                            foreach (var responseExecuteHistoryDay in responseAPIExecuteHistorySQLiteDays)
                            {
                                if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                                {
                                    summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                    summary.CountFail += responseExecuteHistoryDay.CountFail;
                                }
                            }
                        }
                    }

                    var responseAPIExecuteHistorySQLiteDaysCurrent = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryDayToDaySQLite(request.Body);
                    if (responseAPIExecuteHistorySQLiteDaysCurrent != null)
                    {
                        foreach (var responseExecuteHistoryDay in responseAPIExecuteHistorySQLiteDaysCurrent)
                        {
                            if (summaryDict.TryGetValue(responseExecuteHistoryDay.Hour, out var summary))
                            {
                                summary.CountSuccess += responseExecuteHistoryDay.CountSuccess;
                                summary.CountFail += responseExecuteHistoryDay.CountFail;
                            }
                        }
                    }
                }

                return summaryDict.Values.ToList();
            });
        }

        [HttpPost("GetApiThirdPartyExecuteHistoryDaySQLite")]
        [IBoxAuthorization]
        public List<ResponseExecuteHistoryDay> GetApiThirdPartyExecuteHistoryDaySQLite(RequestExecuteHistoryDayOrMonth request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            DateTime dateTime = _commonData.ToDate1(request.SelectDate);
            List<ResponseExecuteHistoryDay> historyAPIDaySQLServers = new List<ResponseExecuteHistoryDay>();
            historyAPIDaySQLServers = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryDayToDaySQLite(request);
            return historyAPIDaySQLServers;
        }

        [HttpPost("GetApiThirdPartyExecuteHistoryMonth")]
        [IBoxAuthorization]
        [IBoxActionPermission("home-api-view", "history-api-third-party")]
        public ResponseForm<List<ResponseExecuteHistoryMonth>> GetApiThirdPartyExecuteHistoryMonth(RequestForm<RequestExecuteHistoryDayOrMonth> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<List<ResponseExecuteHistoryMonth>>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();
                //Lấy data từ history sql server
                var historyMonthSQLServers = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryMonthSQLServer(request.Body);
                //lấy data theo ngày từ sqlite
                foreach (var urlLog in IBGlobalConfig.LogServiceIBox.Where(ptr => !ptr.Contains(IBGlobalConfig.ThisSite)))
                {
                    string urlLogService = string.Format(@"{0}{1}",
                          urlLog,
                          UrlServiceIBConfig.UrlGetAPIStatusMonthSQLite
                        );
                    ResponseExecuteHistoryMonth? responseAPIExecuteHistorySQLiteMonth = _handelWFAndAPI.CallGetDataHistory<ResponseExecuteHistoryMonth>(request.Body, urlLogService, authorization, tenantID);

                    if (responseAPIExecuteHistorySQLiteMonth != null)
                    {
                        var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == responseAPIExecuteHistorySQLiteMonth.Date);
                        if (historyMonthSQLServer != null)
                        {
                            historyMonthSQLServer.CountSuccess += responseAPIExecuteHistorySQLiteMonth.CountSuccess;
                            historyMonthSQLServer.CountFail += responseAPIExecuteHistorySQLiteMonth.CountFail;
                        }
                    }
                }

                //Call api đến server còn lại lấy data và gộp data hiện tại
                var historyMonthToDaySQLite = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryMonthToDaySQLite(request.Body);
                if (historyMonthToDaySQLite != null)
                {
                    var historyMonthSQLServer = historyMonthSQLServers.FirstOrDefault(ptr => ptr.Date == historyMonthToDaySQLite.Date);
                    if (historyMonthSQLServer != null)
                    {
                        historyMonthSQLServer.CountSuccess += historyMonthToDaySQLite.CountSuccess;
                        historyMonthSQLServer.CountFail += historyMonthToDaySQLite.CountFail;
                    }
                }

                return historyMonthSQLServers;
            });
        }

        [HttpPost("GetApiThirdPartyExecuteHistoryMonthSQLiteDetails")]
        [IBoxAuthorization]
        public ResponseExecuteHistoryMonth GetApiThirdPartyExecuteHistoryMonthDetails(RequestExecuteHistoryDayOrMonth request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            var historyMonthToDaySQLite = _handelWFAndAPI.GetApiThirdPartyExecuteHistoryMonthToDaySQLite(request);
            return historyMonthToDaySQLite;
        }

        #endregion API History

        #region Lấy lịch sử data chi tiết API và WF và SQL

        [HttpPost("GetAllApiThirdPartyExecuteHistories")]
        [IBoxAuthorization]
        [IBoxActionPermission("history-api-third-party")]
        public ResponseForm<ResponseAllApiThirdPartyExecuteHistory> GetAllApiThirdPartyExecuteHistories(RequestForm<RequestGetAllExecuteThirdPartyHistory> request)
        {
            int pageSize = 30;
            int skipCount = (request.Body.PageNum - 1) * pageSize;
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<ResponseAllApiThirdPartyExecuteHistory>(() =>
            {
                int pageSize = 30;
                int skipCount = (request.Body.PageNum - 1) * pageSize;

                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();

                List<ApiThirdPartyExecuteHistory> allData = new();
                int totalRecords = 0;

                List<ApiThirdPartyExecuteHistory> FetchDataFromServer(string url, int skip, int take)
                {
                    request.Body.Skip = skip;
                    request.Body.Take = take;
                    ResponseAllApiThirdPartyExecuteHistory? response = new ResponseAllApiThirdPartyExecuteHistory();
                    if (url.Contains(IBGlobalConfig.ThisSite))
                    {
                        response = _handelWFAndAPI.GetAllApiThirdPartyExecuteHistories(request.Body);
                    }
                    else
                    {
                        string urlLogService = $"{url}{UrlServiceIBConfig.UrlGetAPIHistorySQLiteDetails}";
                        response = _handelWFAndAPI.CallGetDataHistory<ResponseAllApiThirdPartyExecuteHistory>(request.Body, urlLogService, authorization, tenantID);
                    }

                    if (response?.Data == null) return new List<ApiThirdPartyExecuteHistory>();
                    Interlocked.Add(ref totalRecords, response.TotalReCords);
                    return response.Data;
                }

                foreach (var urlLog in IBGlobalConfig.LogServiceIBox)
                {
                    int fetchSize = skipCount + pageSize;
                    var serverData = FetchDataFromServer(urlLog, 0, fetchSize);
                    allData.AddRange(serverData);
                }

                var orderedData = allData.OrderByDescending(x => x.TimeStart).ToList();

                var paginatedData = orderedData.Skip(skipCount).Take(pageSize).ToList();

                var response = new ResponseAllApiThirdPartyExecuteHistory
                {
                    Data = paginatedData,
                    TotalReCords = totalRecords
                };

                return response;
            });
        }

        [HttpPost("GetApiThirdPartyExecuteHistoriesDetail")]
        [IBoxAuthorization]
        public ResponseAllApiThirdPartyExecuteHistory GetApiThirdPartyExecuteHistoriesDetail(RequestGetAllExecuteThirdPartyHistory request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            return _handelWFAndAPI.GetAllApiThirdPartyExecuteHistories(request);
        }

        [HttpPost("GetAllWorkflowExecuteHistory")]
        [IBoxAuthorization]
        [IBoxActionPermission("history-workflow")]
        public ResponseForm<ResponseAllWorkflowExecuteHistory> GetAllWorkflowExecuteHistory(RequestForm<RequestGetAllExecuteHistory> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<ResponseAllWorkflowExecuteHistory>(() =>
            {
                int pageSize = 30;
                int skipCount = (request.Body.PageNum - 1) * pageSize;

                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();
                List<WorkflowExecuteHistory> allData = new();
                int totalRecords = 0;
                List<WorkflowExecuteHistory> FetchDataFromServer(string url, int skip, int take)
                {
                    request.Body.Skip = skip;
                    request.Body.Take = take;
                    ResponseAllWorkflowExecuteHistory? response = new ResponseAllWorkflowExecuteHistory();
                    if (url.Contains(IBGlobalConfig.ThisSite))
                    {
                        response = _handelWFAndAPI.GetAllWorkflowExecuteHistories(request.Body);
                    }
                    else
                    {
                        string urlLogService = $"{url}{UrlServiceIBConfig.UrlGetWFHistorySQLiteDetails}";
                        response = _handelWFAndAPI.CallGetDataHistory<ResponseAllWorkflowExecuteHistory>(request.Body, urlLogService, authorization, tenantID);
                    }

                    if (response?.Data == null) return new List<WorkflowExecuteHistory>();
                    Interlocked.Add(ref totalRecords, response.TotalReCords);
                    return response.Data;
                }

                foreach (var urlLog in IBGlobalConfig.LogServiceIBox)
                {
                    int fetchSize = skipCount + pageSize;
                    var serverData = FetchDataFromServer(urlLog, 0, fetchSize);
                    allData.AddRange(serverData);
                }

                var orderedData = allData.OrderByDescending(x => x.TimeStart).ToList();
                var paginatedData = orderedData.Skip(skipCount).Take(pageSize).ToList();
                var responseAllWorkflowExecuteHistory = new ResponseAllWorkflowExecuteHistory
                {
                    Data = paginatedData,
                    TotalReCords = totalRecords
                };

                return responseAllWorkflowExecuteHistory;
            });
        }

        [HttpPost("GetWorkflowExecuteHistoryDetail")]
        [IBoxAuthorization]
        public ResponseAllWorkflowExecuteHistory GetWorkflowExecuteHistoryDetail(RequestGetAllExecuteHistory request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            return _handelWFAndAPI.GetAllWorkflowExecuteHistories(request);
        }

        [HttpPost("GetAllQuerySQLExecuteHistory")]
        [IBoxAuthorization]
        public ResponseForm<ResponseGetAllExecuteQuerySQLHistory> GetAllQuerySQLExecuteHistory(RequestForm<RequestGetAllExecuteQuerySQLHistory> request)
        {
            string address = IBGlobalConfig.ThisSite;
            string[] parts = address.Split('.');
            var result = parts.Length > 0 ? parts[parts.Length - 1] : address;
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("IBox", result);
            }

            return new ResponseForm<ResponseGetAllExecuteQuerySQLHistory>(() =>
            {
                int pageSize = 30;
                int skipCount = (request.Body.PageNum - 1) * pageSize;

                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                request.Body.TenantId = tenantID;
                var authorization = Request.Headers["Authorization"].ToString();

                List<ExecuteQuerySQLHistory> allData = new();
                int totalRecords = 0;

                List<ExecuteQuerySQLHistory> FetchDataFromServer(string url, int skip, int take)
                {
                    request.Body.Skip = skip;
                    request.Body.Take = take;

                    ResponseGetAllExecuteQuerySQLHistory? response = new ResponseGetAllExecuteQuerySQLHistory();
                    if (url.Contains(IBGlobalConfig.ThisSite))
                    {
                        response = _handelWFAndAPI.GetAllQuerySQLExecuteHistories(request.Body);
                    }
                    else
                    {
                        string urlLogService = $"{url}{UrlServiceIBConfig.GetQuerySQLExecuteHistoryDetail}";
                        response = _handelWFAndAPI.CallGetDataHistory<ResponseGetAllExecuteQuerySQLHistory>(request.Body, urlLogService, authorization, tenantID);
                    }

                    if (response?.Data == null) return new List<ExecuteQuerySQLHistory>();
                    Interlocked.Add(ref totalRecords, response.TotalReCords);
                    return response.Data;
                }

                foreach (var urlLog in IBGlobalConfig.LogServiceIBox)
                {
                    int fetchSize = skipCount + pageSize;
                    var serverData = FetchDataFromServer(urlLog, 0, fetchSize);
                    allData.AddRange(serverData);
                }

                var orderedData = allData.OrderByDescending(x => x.TimeStart).ToList();

                var paginatedData = orderedData.Skip(skipCount).Take(pageSize).ToList();

                var response = new ResponseGetAllExecuteQuerySQLHistory
                {
                    Data = paginatedData,
                    TotalReCords = totalRecords
                };

                return response;
            });
        }

        [HttpPost("GetQuerySQLExecuteHistoryDetail")]
        [IBoxAuthorization]
        public ResponseGetAllExecuteQuerySQLHistory GetQuerySQLExecuteHistoryDetail(RequestGetAllExecuteQuerySQLHistory request)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            request.TenantId = tenantID;
            return _handelWFAndAPI.GetAllQuerySQLExecuteHistories(request);
        }

        #endregion Lấy lịch sử data chi tiết API và WF
    }
}