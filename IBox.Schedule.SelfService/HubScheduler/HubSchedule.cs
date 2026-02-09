using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Quartz;
using Serilog;

namespace IBox.Schedule.SelfService.HubScheduler
{
    internal class SiteMoment
    {
        public string? Site { get; set; }
        public bool Status { get; set; }
        public int Level { get; set; }
    }

    public class HubSchedule : IHubSchedule
    {
        private HubConnection? _connection;
        private static readonly Dictionary<string, HubConnection> _dictConnections = new();
        private static int disconnectCount = 0;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IScheduler _scheduler;
        internal readonly List<SiteMoment> _listSitesMoment = new();

        public HubSchedule(Common.Objects.IConfiguration configuration, IEncryption encryption, IScheduler scheduler)
        {
            _configuration = configuration;
            _encryption = encryption;
            _scheduler = scheduler;
        }

        /// <summary>
        /// Tạo Hub cho schedule
        /// </summary>
        /// <returns></returns>
        public async Task CreateHubSchedule()
        {
            try
            {
                var thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;
                var listServerSite = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == TypeService.ScheduleService).ToList();

                foreach (var serverSite in listServerSite)
                {
                    if (serverSite.Site == thisSite)
                    {
                        continue;
                    }

                    string urlHub = $"{BuildUrlHub(serverSite)}/ChatHubSchedule";

                    if (!CheckHubReconnected(urlHub))
                    {
                        await CreateHubToServer(thisSite, urlHub);

                        if (_connection != null && _connection.State == HubConnectionState.Connected)
                        {
                            RegisterReceiveMessageHandler((user, _message) => { });
                        }
                    }

                    if (!CheckHubReconnected(urlHub))
                    {
                        Log.Information($"Server at {urlHub} is disconnected.");

                        UpdateDisconnectCount();

                        var rootContext = new RootContext(this._configuration, this._encryption);

                        var siteDisconnected = rootContext.Context.S_ServerScheduleStatus.FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == serverSite.Site);

                        if (siteDisconnected == null)
                        {
                            Log.Error($"Not found server at {serverSite.Site}.");
                            continue;
                        }

                        Log.Information($"Update hub {urlHub} false.");

                        rootContext.Context.Database.ExecuteSqlInterpolated($"EXEC sp_UpdateStatusServerSchedule 'false', {siteDisconnected.Site}");

                        rootContext.Dispose();
                    }

                    if (disconnectCount >= 5)
                    {
                        Thread.Sleep(1000);

                        Log.Information($"Server at {urlHub} has been disconnected 5 times.");

                        var rootContextAlfter = new RootContext(this._configuration, this._encryption);

                        var siteDisconnectedAlfter = rootContextAlfter.Context.S_ServerScheduleStatus.FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == serverSite.Site);

                        rootContextAlfter.Context.Dispose();

                        if (siteDisconnectedAlfter == null)
                        {
                            ResetDisconnectCount();

                            continue;
                        }

                        if (siteDisconnectedAlfter.Status == true)
                        {
                            Log.Information($"server hub {serverSite.Site} is running.");

                            UpdateSiteMoment(serverSite.Site ?? "", true, serverSite.Level ?? "0");

                            ResetDisconnectCount();

                            continue;
                        }

                        Log.Information($"server hub {serverSite.Site} is not running.");

                        UpdateSiteMoment(serverSite.Site ?? "", false, serverSite.Level ?? "0");

                        ResetDisconnectCount();

                        UpdateJobRunForAllTenants(thisSite);
                    }
                }

                foreach (var item in _dictConnections)
                {
                    Log.Information($"hub: {item.Key}, State: {item.Value.State}, ConnectionId: {item.Value.ConnectionId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateHubSchedule an error has occurred: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật số lần disconnect của hub
        /// </summary>
        private static void UpdateDisconnectCount()
        {
            disconnectCount++;
        }

        /// <summary>
        /// Cài lại số lần disconect của hub
        /// </summary>
        private static void ResetDisconnectCount()
        {
            disconnectCount = 0;
        }

        /// <summary>
        /// Tạo hub theo urlHub
        /// </summary>
        /// <param name="thisSite"></param>
        /// <param name="urlHub"></param>
        /// <returns></returns>
        public async Task CreateHubToServer(string thisSite, string urlHub)
        {
            try
            {
                if (_connection == null || _connection.State != HubConnectionState.Connected)
                {
                    _connection = new HubConnectionBuilder()
                           .WithUrl(urlHub)
                           .Build();

                    AddConnectionToHub(urlHub, _connection);

                    _connection.Closed += async (error) =>
                    {
                        Log.Error($"Connection closed from: {thisSite} to urlHub: {urlHub}, error: {error?.Message}");
                        await RemoveConnectionFromHub(urlHub);
                    };

                    await _connection.StartAsync();

                    await SendMessage($"I am {thisSite}", urlHub);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ConnectToServer hub {urlHub} an error has occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Sự kiện gửi tin nhắn tới hub
        /// </summary>
        /// <param name="message"></param>
        /// <param name="serverUrl"></param>
        /// <returns></returns>
        private async Task SendMessage(string message, string serverUrl)
        {
            try
            {
                if (_connection != null && _connection.State == HubConnectionState.Connected)
                {
                    await _connection.InvokeAsync("SendMessage", message + $", ConnectionId: {_connection.ConnectionId}");
                }
                else
                {
                    Log.Error($"Connection is closed with url {serverUrl}.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SendMessage an error has occurred: {ex}");
            }
        }

        /// <summary>
        /// Đăng ký sự kiện gửi tin nhắn tới hub
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterReceiveMessageHandler(Action<string, string> handler)
        {
            _connection?.On<string, string>("ReceiveMessage", handler);
        }

        /// <summary>
        /// Thêm hub kết nối vào cache
        /// </summary>
        /// <param name="hubUrl"></param>
        /// <param name="connection"></param>
        private static void AddConnectionToHub(string hubUrl, HubConnection connection)
        {
            Log.Information($"AddConnectionToHub with hubUrl: {hubUrl}");

            if (_dictConnections.ContainsKey(hubUrl))
            {
                _dictConnections.Remove(hubUrl);
            }

            _dictConnections.Add(hubUrl, connection);
        }

        /// <summary>
        /// Xóa hub mất kết nối khỏi cache
        /// </summary>
        /// <param name="hubUrl"></param>
        /// <returns></returns>
        private static async Task RemoveConnectionFromHub(string hubUrl)
        {
            if (_dictConnections.ContainsKey(hubUrl))
            {
                HubConnection connection = _dictConnections[hubUrl];

                Log.Information($"RemoveConnectionFromHub hubUrl: {hubUrl}, State: {connection.State}");

                await connection.DisposeAsync();

                _dictConnections.Remove(hubUrl);
            }

            Log.Information($"RemoveConnectionFromHub not found hubUrl: {hubUrl}");
        }

        /// <summary>
        /// Kiểm tra trạng thái connect của hub
        /// </summary>
        /// <param name="urlHub"></param>
        /// <returns></returns>
        public bool CheckHubReconnected(string urlHub)
        {
            return _dictConnections.TryGetValue(urlHub, out var connection) &&
                   connection.State == HubConnectionState.Connected &&
                   connection.ConnectionId != null;
        }

        /// <summary>
        /// Cập nhật trạng thái site hiện tại vào cache
        /// </summary>
        /// <param name="site"></param>
        /// <param name="status"></param>
        /// <param name="level"></param>
        private void UpdateSiteMoment(string site, bool status, string level)
        {
            if (string.IsNullOrEmpty(site))
            {
                return;
            }

            var siteInfo = _listSitesMoment.Find(ptr => ptr.Site == site);

            if (siteInfo != null)
            {
                _listSitesMoment.Remove(siteInfo);
            }

            _listSitesMoment.Add(new SiteMoment()
            {
                Site = site,
                Level = Int32.Parse(level),
                Status = status
            });
        }

        /// <summary>
        /// Cập nhật chạy job cho toàn bộ tenant
        /// </summary>
        /// <param name="thisSite"></param>
        private void UpdateJobRunForAllTenants(string thisSite)
        {
            var rootContext = new RootContext(this._configuration, this._encryption);

            var listTenant = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

            rootContext.Context.Dispose();

            foreach (var tenant in listTenant)
            {
                foreach (var siteMoment in _listSitesMoment)
                {
                    if (!siteMoment.Status)
                    {
                        string siteConfigToRun = GetResultIp(siteMoment.Site ?? "", _listSitesMoment, thisSite);

                        RunJobServer(siteMoment.Site ?? "", siteConfigToRun, listTenant, thisSite);
                    }
                }
            }
        }

        /// <summary>
        /// Chia site chạy cho server
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="ipLevels"></param>
        /// <param name="defaultIp"></param>
        /// <returns></returns>
        static string GetResultIp(string targetIp, List<SiteMoment> ipLevels, string defaultIp)
        {
            var targetIpInfo = ipLevels.Find(ipInfo => ipInfo.Site == targetIp);

            if (targetIpInfo != default)
            {
                var higherLevelIps = ipLevels.Where(ipInfo => ipInfo.Level > targetIpInfo.Level && ipInfo.Status).ToList();
                var lowerLevelIps = ipLevels.Where(ipInfo => ipInfo.Level < targetIpInfo.Level && ipInfo.Status).ToList();

                if (higherLevelIps.Any())
                {
                    var highestLevelIp = higherLevelIps.OrderBy(ipInfo => ipInfo.Level).First();
                    return highestLevelIp?.Site ?? defaultIp;
                }
                else if (lowerLevelIps.Any())
                {
                    var lowestLevelIp = lowerLevelIps.OrderByDescending(ipInfo => ipInfo.Level).First();
                    return lowestLevelIp?.Site ?? defaultIp;
                }
            }

            return defaultIp;
        }

        /// <summary>
        /// Chạy job sau khi được phân bổ cho site
        /// </summary>
        /// <param name="siteError"></param>
        /// <param name="siteRun"></param>
        /// <param name="listTenant"></param>
        private void RunJobServer(string siteError, string siteRun, List<T_Tenant> listTenant, string thisSite)
        {
            Log.Information($"Run switch job server Started. SiteError: {siteError}, SiteRun: {siteRun}");

            for (int i = 0; i < listTenant.Count; i++)
            {
                var itemTenants = listTenant[i];

                if (siteRun != thisSite)
                {
                    continue;
                }

                var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

                var tenantContext = context.GetTenantContext(itemTenants.Id).Context;

                var listPlanScheduler = tenantContext.S_ImplementationPlans.Where(ptr => !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date && ptr.SiteRunning == siteError).ToList();

                tenantContext.Context.Dispose();

                UpdateImplementationPlans(listPlanScheduler, itemTenants.Id, siteError, siteRun);
            }
        }

        /// <summary>
        /// Cập nhật phân bổ site
        /// </summary>
        /// <param name="listPlanScheduler"></param>
        /// <param name="tennantId"></param>
        /// <param name="siteError"></param>
        /// <param name="siteRun"></param>
        private void UpdateImplementationPlans(List<S_ImplementationPlan> listPlanScheduler, string tennantId, string siteError, string siteRun)
        {
            var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = context.GetTenantContext(tennantId).Context;

            foreach (var itemPlanScheduler in listPlanScheduler)
            {
                try
                {
                    if (_scheduler.IsStarted)
                    {
                        var jobKey = new JobKey($"Job_{itemPlanScheduler.ScheduleID}_{siteError}", "Group2");
                        var jobTrigger = new TriggerKey($"Trigg_{itemPlanScheduler.ScheduleID}_{siteError}", "Group2");

                        bool isJobExists = _scheduler.CheckExists(jobKey).Result;
                        if (isJobExists)
                        {
                            _scheduler.DeleteJob(jobKey);
                        }

                        string ConvertCron = !string.IsNullOrEmpty(itemPlanScheduler.FormatCron) ? itemPlanScheduler.FormatCron : $"{itemPlanScheduler.Second} {itemPlanScheduler.Minute} {itemPlanScheduler.Hour} ? {itemPlanScheduler.Month} {itemPlanScheduler.Day} {itemPlanScheduler.Year}";

                        var job = JobBuilder.Create<ExcuteWorkflow>()
                            .UsingJobData("wfid", itemPlanScheduler.Wfid)
                            .UsingJobData("planId", itemPlanScheduler.Id)
                            .UsingJobData("scheduleId", itemPlanScheduler.ScheduleID)
                            .UsingJobData("tenantId", tennantId)
                            .UsingJobData("typeSchedule", ((int?)itemPlanScheduler.Type).ToString())
                            .UsingJobData("jobKey", jobKey.Name)
                            .UsingJobData("isDeploy", "False")
                            .WithIdentity(jobKey)
                            .Build();

                        ITrigger trigger = TriggerBuilder.Create()
                            .WithIdentity(jobTrigger)
                            .WithCronSchedule(ConvertCron)
                            .Build();
                        _scheduler.ScheduleJob(job, trigger);

                        tenantContext.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == itemPlanScheduler.Id).TryUpdate(tenantContext, new S_ImplementationPlan()
                        {
                            Id = itemPlanScheduler.Id,
                            StatusPlan = StatusPlan.Complete,
                            KeyTrigg = $"Job_{itemPlanScheduler.ScheduleID}_{siteError}",
                            SiteRunning = siteRun
                        });

                        Log.Information($"Update planid {itemPlanScheduler.Id} run with site {siteRun}");
                    }
                    else
                    {
                        tenantContext.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == itemPlanScheduler.Id).TryUpdate(tenantContext, new S_ImplementationPlan()
                        {
                            Id = itemPlanScheduler.Id,
                            StatusPlan = StatusPlan.Await
                        });
                    }
                }
                catch (Exception ex)
                {
                    tenantContext.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == itemPlanScheduler.Id).TryUpdate(tenantContext, new S_ImplementationPlan()
                    {
                        Id = itemPlanScheduler.Id,
                        StatusPlan = StatusPlan.Fail,
                    });
                    Log.Error("RunJobServer plan error: " + JsonConvert.SerializeObject(itemPlanScheduler));
                    Log.Error($"RunJobServer an error has occurred when ping server: {ex}");
                }
            }

            tenantContext.Context.Dispose();
        }

        /// <summary>
        /// Trả về url hub
        /// </summary>
        /// <param name="s_Service"></param>
        /// <returns></returns>
        private string BuildUrlHub(S_Service s_Service)
        {
            string host = $"{s_Service.TypeProtocol}://{s_Service.Site ?? string.Empty}";
            if (!string.IsNullOrEmpty(s_Service.Port))
            {
                host = $"{host}:{s_Service.Port}";
            }

            if (!string.IsNullOrEmpty(s_Service.SubDomain))
            {
                host = $"{host}/{s_Service.SubDomain}";
            }

            return host;
        }
    }
}
