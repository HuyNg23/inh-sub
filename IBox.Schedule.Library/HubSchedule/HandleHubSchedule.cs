using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.Commom;
using IBox.Schedule.Library.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace IBox.Schedule.Library.HubSchedule
{
    internal class SiteMoment
    {
        public string? Site { get; set; }
        public bool Status { get; set; }
        public int Level { get; set; }
    }

    public class HandleHubSchedule : IHandleHubSchedule
    {
        private HubConnection? _connection;
        private static readonly Dictionary<string, HubConnection> _dictConnections = new();
        private static int disconnectCount = 0;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;
        internal readonly List<SiteMoment> _listSitesMoment = new();
        private readonly ICommonFunction _commonFunction;

        public HandleHubSchedule(IConfiguration configuration, IEncryption encryption, ICommonFunction commonFunction)
        {
            _configuration = configuration;
            _encryption = encryption;
            _commonFunction = commonFunction;
        }

        /// <summary>
        /// Tạo Hub cho schedule
        /// </summary>
        /// <returns></returns>
        public async Task CreateHubSchedule(TypeUserBase typeUser)
        {
            try
            {
                var thisSite = IBGlobalConfig.ThisSite ?? string.Empty;

                if (typeUser == TypeUserBase.Tenant)
                {
                    await CreateHubScheduleTenant(thisSite);
                }
                else if (typeUser == TypeUserBase.Root)
                {
                    await CreateHubScheduleRoot(thisSite);
                }
                else
                {
                    Log.Error($"Not found typeUserBase {typeUser}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateHubSchedule an error has occurred: {ex.Message}", ex);
            }
        }

        public async Task CreateHubScheduleTenant(string thisSite)
        {
            var listServerSite = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == TypeService.ScheduleService).ToList();
            foreach (var serverSite in listServerSite)
            {
                if (serverSite.Site == thisSite)
                {
                    continue;
                }

                string urlHub = $"{_commonFunction.BuildUrlHub(serverSite)}/ChatHubSchedule";

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

                    var rootContext = new RootContext(_configuration, _encryption);

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

                    var rootContextAlfter = new RootContext(_configuration, _encryption);

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

        public async Task CreateHubScheduleRoot(string thisSite)
        {
            try
            {
                var listServerSite = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == TypeService.RootBE).ToList();

                foreach (var serverSite in listServerSite)
                {
                    if (serverSite.Site == thisSite)
                    {
                        continue;
                    }
                    string urlHub = $"{_commonFunction.BuildUrlHub(serverSite)}/ChatHubSchedule";

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

                        var rootContext = new RootContext(_configuration, _encryption);

                        var siteDisconnected = rootContext.Context.S_ServerScheduleShrinkLogStates.FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == serverSite.Site);

                        if (siteDisconnected == null)
                        {
                            Log.Error($"Not found server at {serverSite.Site}.");
                            continue;
                        }

                        Log.Information($"Update hub {urlHub} false.");

                        rootContext.Context.Database.ExecuteSqlInterpolated($"EXEC sp_UpdateStatusServerShrinkLog 'false', {siteDisconnected.Site}");

                        rootContext.Dispose();
                    }

                    if (disconnectCount >= 5)
                    {
                        Thread.Sleep(1000);

                        Log.Information($"Server at {urlHub} has been disconnected 5 times.");

                        var rootContextAlfter = new RootContext(_configuration, _encryption);

                        var siteDisconnectedAlfter = rootContextAlfter.Context.S_ServerScheduleShrinkLogStates.FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == serverSite.Site);

                        rootContextAlfter.Context.Dispose();

                        if (siteDisconnectedAlfter == null)
                        {
                            Log.Information($"Not found site disconnect {serverSite.Site}, please review config ServerScheduleShrinkLogStates");

                            ResetDisconnectCount();

                            continue;
                        }

                        if (siteDisconnectedAlfter.Status)
                        {
                            Log.Information($"server hub {serverSite.Site} is running.");

                            UpdateSiteMoment(serverSite.Site ?? "", true, serverSite.Level ?? "0");

                            ResetDisconnectCount();

                            continue;
                        }

                        Log.Information($"server hub {serverSite.Site} is not running.");

                        UpdateSiteMoment(serverSite.Site ?? "", false, serverSite.Level ?? "0");

                        ResetDisconnectCount();

                        UpdateJobRunForAllRoot(thisSite);
                    }
                }

                foreach (var item in _dictConnections)
                {
                    Log.Information($"hub: {item.Key}, State: {item.Value.State}, ConnectionId: {item.Value.ConnectionId}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateHubSchedule an error has occurred: {ex}");
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
            try
            {
                //var rootContext = new RootContext(_configuration, _encryption);

                //var listTenant = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

                //rootContext.Context.Dispose();

                var listTenant = IBGlobalConfig.Tenants;

                foreach (var tenant in listTenant)
                {
                    foreach (var siteMoment in _listSitesMoment)
                    {
                        if (!siteMoment.Status)
                        {
                            string siteConfigToRun = GetResultIp(siteMoment.Site ?? "", _listSitesMoment, thisSite);

                            RunJobServerTenant(siteMoment?.Site ?? "", siteConfigToRun, listTenant, thisSite);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateJobRunForAllTenants:{ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật chạy job cho toàn bộ Root
        /// </summary>
        /// <param name="thisSite"></param>
        private void UpdateJobRunForAllRoot(string thisSite)
        {
            try
            {
                foreach (var siteMoment in _listSitesMoment)
                {
                    if (!siteMoment.Status)
                    {
                        string siteConfigToRun = GetResultIp(siteMoment.Site ?? "", _listSitesMoment, thisSite);

                        RunJobServerRoot(siteMoment.Site ?? "", siteConfigToRun, thisSite);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateJobRunForAllRoot: {ex.Message}\n thisSite: {thisSite}");
            }
        }

        /// <summary>
        /// Chia site chạy cho server
        /// </summary>
        /// <param name="targetIp"></param>
        /// <param name="ipLevels"></param>
        /// <param name="defaultIp"></param>
        /// <returns></returns>
        private static string GetResultIp(string targetIp, List<SiteMoment> ipLevels, string defaultIp)
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
        /// Chạy job sau khi được phân bổ cho site tenant
        /// </summary>
        /// <param name="siteError"></param>
        /// <param name="siteRun"></param>
        /// <param name="listTenant"></param>
        private void RunJobServerTenant(string siteError, string siteRun, List<T_Tenant> listTenant, string thisSite)
        {
            try
            {
                Log.Information($"Run switch job server Started. SiteError: {siteError}, SiteRun: {siteRun}, listTenant: {JsonConvert.SerializeObject(listTenant)}");

                for (int i = 0; i < listTenant.Count; i++)
                {
                    var itemTenants = listTenant[i];

                    if (siteRun != thisSite)
                    {
                        continue;
                    }

                    var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));

                    var tenantContext = context.GetTenantContext(itemTenants.Id).Context;

                    var listScheduler = tenantContext.S_Schedules.Where(ptr =>
                                                                            !ptr.IsDelete
                                                                            && ptr.Site == siteError)
                                                                            .ToList();

                    tenantContext.Context.Dispose();
                    UpdateImplementationSchedule(listScheduler, itemTenants.Id, siteError, siteRun);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RunJobServerTenant: {ex.Message} \n SiteError: {siteError}, SiteRun: {siteRun}, listTenant: {JsonConvert.SerializeObject(listTenant)}");
            }
        }

        /// <summary>
        /// Chạy job sau khi được phân bổ cho site root
        /// </summary>
        /// <param name="siteError"></param>
        /// <param name="siteRun"></param>
        /// <param name="thisSite"></param>
        private void RunJobServerRoot(string siteError, string siteRun, string thisSite)
        {
            try
            {
                Log.Information($"Run switch job server Started. SiteError: {siteError}, SiteRun: {siteRun}");

                if (siteRun != thisSite)
                {
                    return;
                }

                var rootContext = new RootContext(_configuration, _encryption).Context;

                var listScheduler = rootContext.S_ScheduleShrinkLogs.Where(ptr =>
                                        !ptr.IsDelete
                                        && ptr.Site == siteError).ToList();

                rootContext.Dispose();

                UpdateImplementationScheduleRoot(listScheduler, siteError, siteRun);
            }
            catch (Exception ex)
            {
                Log.Error($"RunJobServerRoot: {ex.Message} \n siteError: {siteError}, siteRun: {siteRun}, thisSite: {thisSite}");
            }
        }

        private void UpdateImplementationScheduleRoot(List<S_ScheduleShrinkLog> lst_ScheduleShrink, string siteError, string siteRun)
        {
            try
            {
                var rootContext = new RootContext(_configuration, _encryption).Context;

                if (!_commonFunction.CheckScheduleStart())
                {
                    _commonFunction.StartSchedule();
                }

                var connectDBRoot = rootContext.D_DatabaseConnections.FirstOrDefault(ptr => !ptr.IsDelete);
                if (connectDBRoot == null)
                {
                    new IboxLog("connectDB Root is null", "AppLogs", "Error");
                    return;
                }

                foreach (var scheduleShrink in lst_ScheduleShrink)
                {
                    try
                    {
                        var scheduleBase = _commonFunction.ConvertToScheduleBase(scheduleShrink);
                        var cronExpression = _commonFunction.GetCronExpression(scheduleBase);

                        var reqCreateJob = new ReqCreateScheduleJob()
                        {
                            ScheduleId = scheduleShrink.Id,
                            ListCategory = scheduleShrink.ListCategory,
                            Site = siteError,
                            CronExpression = cronExpression,
                            MethodShrinkLog = scheduleShrink.MethodShrinkLog,
                            TypeSchedule = (Database.Tenant.Tables.TypeScheduleBase?)scheduleShrink.Type,
                            DeploymentType = connectDBRoot.DeploymentType,
                            JobName = scheduleShrink.Name
                        };

                        _commonFunction.CreateScheduleJobRoot(reqCreateJob);

                        new IboxLog($"Update planid {scheduleShrink.Id} run with site {siteRun}", "AppLogs", "Info");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"RunJobServer an error has occurred when ping server: {ex} \n lst_ScheduleShrink: {JsonConvert.SerializeObject(lst_ScheduleShrink)}, siteError: {siteError}, siteRun: {siteRun}");
                    }
                }

                rootContext.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateImplementationScheduleRoot: {ex.Message} \n lst_ScheduleShrink: {JsonConvert.SerializeObject(lst_ScheduleShrink)}, siteError: {siteError}, siteRun: {siteRun}");
            }
        }

        /// <summary>
        /// Cập nhật phân bổ site
        /// </summary>
        /// <param name="listScheduler"></param>
        /// <param name="tenantId"></param>
        /// <param name="siteError"></param>
        /// <param name="siteRun"></param>
        private void UpdateImplementationSchedule(List<S_Schedule> listScheduler, string tenantId, string siteError, string siteRun)
        {
            try
            {
                Log.Information($"UpdateImplementationSchedule: listScheduler: {JsonConvert.SerializeObject(listScheduler)}, tenantId: {tenantId}, siteError: {siteError}");
                if (!_commonFunction.CheckScheduleStart())
                {
                    _commonFunction.StartSchedule();
                }

                if (_commonFunction.CheckScheduleStart())
                {
                    foreach (var schedule in listScheduler)
                    {
                        try
                        {
                            var nameJobKey = _commonFunction.FormatNameJobKey(schedule.Name ?? "");
                            string nameTrigger = _commonFunction.FormatNameTrigger(schedule.Id ?? "", siteError ?? "", tenantId);

                            bool isJobExists = _commonFunction.CheckExistsJobInSchedule(nameJobKey);

                            if (isJobExists)
                            {
                                _commonFunction.DeleteJobSchedule(schedule);
                            }

                            var scheduleBase = _commonFunction.ConvertToScheduleBase(schedule);
                            var cronExpression = _commonFunction.GetCronExpression(scheduleBase);

                            var reqCreateJob = new ReqCreateScheduleJob()
                            {
                                WfId = schedule.Wfid,
                                ScheduleId = schedule.Id,
                                TenantId = tenantId,
                                Site = schedule.Site,
                                CronExpression = cronExpression,
                                TypeSchedule = schedule.Type,
                                JobName = schedule.Name
                            };
                            Log.Information($"reqCreateJob: {JsonConvert.SerializeObject(reqCreateJob)}");
                            _commonFunction.CreateScheduleJobTenant(reqCreateJob);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"RunJobServer an error has occurred when ping server: {ex}\n schedule: {JsonConvert.SerializeObject(schedule)} \n listScheduler: {JsonConvert.SerializeObject(listScheduler)}, tenantId: {tenantId}, siteError: {siteError}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateImplementationSchedule: {ex.Message}\n listScheduler: {JsonConvert.SerializeObject(listScheduler)}, tenantId: {tenantId}, siteError: {siteError}");
            }
        }
    }
}