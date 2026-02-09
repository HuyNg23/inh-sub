using Azure;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.ServiceIB;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.Commom;
using IBox.Schedule.Library.HubSchedule;
using IBox.Schedule.Library.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Serilog;

namespace IBox.Schedule.Library.HandleJobSchedule
{
    public class HandleJobsSchedule : IHandleJobsSchedule
    {
        private readonly IBContext<TenantContext> _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly ICommonFunction _commonFunction;
        private readonly IHandleHubSchedule _handleHubSchedule;
        private readonly IEncryption _encryption;
        private readonly IScheduler _scheduler;
        private readonly IRestAPI _restAPI;

        public HandleJobsSchedule(IConfiguration configuration, IBContext<TenantContext> tenantContext, ICommonFunction commonFunction, IHandleHubSchedule handleHubSchedule, IEncryption encryption, IScheduler scheduler, IRestAPI restAPI)
        {
            _configuration = configuration;
            _tenantContext = tenantContext;
            _commonFunction = commonFunction;
            _handleHubSchedule = handleHubSchedule;
            _encryption = encryption;
            _scheduler = scheduler;
            _restAPI = restAPI;
        }

        public bool StartJobNowTenant(HttpRequest requestContext, S_Schedule req)
        {
            string tenantId = GetTenantId(requestContext);

            try
            {
                ValidateTenantRequest(req, tenantId);

                string thisSite = IBGlobalConfig.ThisSite ?? string.Empty;
                string authorization = requestContext.Headers["Authorization"].ToString();


                var tenantContext = _tenantContext.GetTenantContext(tenantId).Context;
                var schedule = GetSchedule(tenantContext, req.Site ?? "", req.Id);

                StopExistingJob(authorization, req.Id);

                if (HandleJobExecution(thisSite, req, authorization, tenantId))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Start job now fail because: {ex}", tenantId, ex);
            }
        }

        public bool StartJobNowRoot(HttpRequest requestContext, S_ScheduleShrinkLog req)
        {
            string tenantId = GetTenantId(requestContext);

            try
            {
                ValidateRootRequest(req, tenantId);

                string thisSite = IBGlobalConfig.ThisSite ?? string.Empty;
                string authorization = requestContext.Headers["Authorization"].ToString();

                using (var rootContext = new RootContext(_configuration, _encryption))
                {
                    var schedule = GetRootSchedule(rootContext, req.Site ?? "", req.Id);

                    StopExistingJobRoot(authorization, req.Id);

                    if (HandleJobExecution(thisSite, req, authorization, tenantId))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Start job now fail because: {ex}", tenantId, ex);
            }
        }

        private void ValidateTenantRequest(S_Schedule req, string tenantId)
        {
            if (string.IsNullOrEmpty(req.Site))
            {
                throw new IboxLog($"Site can't null or empty.", tenantId);
            }

            if (string.IsNullOrEmpty(req.Wfid))
            {
                throw new IboxLog($"WF id can't null or empty.", tenantId);
            }
        }

        private void ValidateRootRequest(S_ScheduleShrinkLog req, string tenantId)
        {
            if (string.IsNullOrEmpty(req.Site))
            {
                throw new IboxLog($"Site can't null or empty.", tenantId);
            }

            if (string.IsNullOrEmpty(req.Id))
            {
                throw new IboxLog($"ScheduleId can't null or empty.", tenantId);
            }
        }

        private string GetTenantId(HttpRequest requestContext)
        {
            return requestContext.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
        }

        private S_Schedule GetSchedule(TenantContext tenantContext, string site, string id)
        {
            var tenantId = tenantContext.Context.TenantInfo.Id;
            var schedule = tenantContext.Context.S_Schedules
                .FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == site && ptr.Id == id);

            if (schedule == null)
            {
                throw new IboxLog($"Start job now fail because not found schedule {id}.", tenantId ?? "AppLogs");
            }

            return schedule;
        }

        private S_ScheduleShrinkLog GetRootSchedule(RootContext rootContext, string site, string id)
        {
            var schedule = rootContext.Context.S_ScheduleShrinkLogs
                .FirstOrDefault(ptr => !ptr.IsDelete && ptr.Site == site && ptr.Id == id);

            if (schedule == null)
            {
                throw new IboxLog($"Start job now fail because not found schedule {id}.", "AppLogs");
            }

            return schedule;
        }

        private void StopExistingJob(string authorization, string jobId)
        {
            S_Schedule s_Schedule = new S_Schedule();
            s_Schedule.Id = jobId;
            var bodyStopJob = JsonConvert.SerializeObject(s_Schedule);
            _commonFunction.CallApiStopJobOnRingTenant(bodyStopJob, authorization);
        }

        private void StopExistingJobRoot(string authorization, string jobId)
        {
            S_ScheduleShrinkLog s_ScheduleShrinkLog = new S_ScheduleShrinkLog();
            s_ScheduleShrinkLog.Id = jobId;
            var bodyStopJob = JsonConvert.SerializeObject(s_ScheduleShrinkLog);
            _commonFunction.CallApiStopJobOnRingRoot(bodyStopJob, authorization);
        }

        private bool HandleJobExecution(string thisSite, object req, string authorization, string tenantId)
        {
            var listServerSite = IBGlobalConfig.Services
                .Where(ptr => ptr.TypeService == (req is S_Schedule ? TypeService.ScheduleService : TypeService.RootBE))
                .ToList();

            string site = GetSiteFromRequest(req, tenantId);

            var serverSite = listServerSite.Find(x => x.Site == site);

            if (serverSite == null)
            {
                Log.Error($"Site {site} is not found in list server site.");
                throw new IboxLog($"Site {site} is not found in list server site.", tenantId ?? "AppLogs");
            }

            var bodyReq = JsonConvert.SerializeObject(req);

            if (serverSite.Site == thisSite)
            {
                if (req is S_Schedule)
                {
                    _commonFunction.CallApiRunSpecifiedJobTenant(serverSite, authorization, bodyReq);
                }
                else if (req is S_ScheduleShrinkLog)
                {
                    _commonFunction.CallApiRunSpecifiedJobRoot(serverSite, authorization, bodyReq);
                }

                return true;
            }

            string urlHub = $"{_commonFunction.BuildUrlHub(serverSite)}/ChatHubSchedule";
            bool isReachable = _handleHubSchedule.CheckHubReconnected(urlHub);

            if (isReachable)
            {
                if (req is S_Schedule)
                {
                    _commonFunction.CallApiRunSpecifiedJobTenant(serverSite, authorization, bodyReq);
                }
                else if (req is S_ScheduleShrinkLog)
                {
                    _commonFunction.CallApiRunSpecifiedJobRoot(serverSite, authorization, bodyReq);
                }

                return true;
            }
            else if (listServerSite.Count == 1)
            {
                if (req is S_Schedule)
                {
                    _commonFunction.CallApiRunSpecifiedJobTenant(listServerSite[1], authorization, bodyReq);
                }
                else if (req is S_ScheduleShrinkLog)
                {
                    _commonFunction.CallApiRunSpecifiedJobRoot(listServerSite[1], authorization, bodyReq);
                }

                return true;
            }
            else
            {
                Log.Information("Hub is not reachable.", urlHub);

                foreach (var _serverSite in listServerSite)
                {
                    string urlHubRun = $"{_commonFunction.BuildUrlHub(_serverSite)}/ChatHubSchedule";
                    bool isCheckSiteRun = _handleHubSchedule.CheckHubReconnected(urlHubRun);

                    if (isCheckSiteRun)
                    {
                        if (req is S_Schedule)
                        {
                            _commonFunction.CallApiRunSpecifiedJobTenant(_serverSite, authorization, bodyReq);
                        }
                        else if (req is S_ScheduleShrinkLog)
                        {
                            _commonFunction.CallApiRunSpecifiedJobRoot(_serverSite, authorization, bodyReq);
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        private string GetSiteFromRequest(object req, string tenantId)
        {
            if (req is S_Schedule scheduleReq)
            {
                return scheduleReq.Site ?? "";
            }
            else if (req is S_ScheduleShrinkLog shrinkLogReq)
            {
                return shrinkLogReq.Site ?? "";
            }

            throw new IboxLog("Unknown request type", tenantId ?? "AppLogs");
        }

        public bool RunSpecifiedJobRoot(HttpRequest requestContext, S_ScheduleShrinkLog req)
        {
            string tenantId = GetTenantId(requestContext);

            try
            {
                var scheduleBase = _commonFunction.ConvertToScheduleBase(req);
                var cronExpression = _commonFunction.GetCronExpression(scheduleBase);

                var reqCreateJob = new ReqCreateScheduleJob()
                {
                    ScheduleId = req.Id,
                    ListCategory = req.ListCategory,
                    Site = req.Site,
                    CronExpression = cronExpression,
                    MethodShrinkLog = req.MethodShrinkLog,
                    TypeSchedule = (Database.Tenant.Tables.TypeScheduleBase?)req.Type,
                    JobName = req.Name
                };

                try
                {
                    CreateScheduleRoot(reqCreateJob);
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"RunSpecifiedJobRoot error because: {ex}", tenantId, ex);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"RunSpecifiedJobRoot error because: {ex}", tenantId, ex);
            }
        }

        public bool RunSpecifiedJobTenant(string tenantId, S_Schedule req)
        {
            try
            {
                var scheduleBase = _commonFunction.ConvertToScheduleBase(req);
                var cronExpression = _commonFunction.GetCronExpression(scheduleBase);
                var reqCreateJob = new ReqCreateScheduleJob()
                {
                    WfId = req.Wfid,
                    ScheduleId = req.Id,
                    TenantId = tenantId,
                    Site = req.Site,
                    CronExpression = cronExpression,
                    TypeSchedule = req.Type,
                    JobName = req.Name
                };

                try
                {
                    CreateScheduleTenant(reqCreateJob, tenantId);
                }
                catch (Exception ex)
                {
                    new IboxLog($"RunSpecifiedJob error because: {ex}", tenantId, ex);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"RunSpecifiedJob error because: {ex}", tenantId, ex);
            }
        }

        private void CreateScheduleTenant(ReqCreateScheduleJob plan, string tenantId)
        {
            _commonFunction.CreateScheduleJobTenant(plan);
        }

        private void CreateScheduleRoot(ReqCreateScheduleJob plan)
        {
            _commonFunction.CreateScheduleJobRoot(plan);
        }

        public bool StopJobScheduleTenant(HttpRequest requestContext, ReqJobSchedule req)
        {
            try
            {
                var authorization = requestContext.Headers["Authorization"].ToString();
                S_Schedule s_Schedule = new S_Schedule();
#pragma warning disable CS8601 // Possible null reference assignment.
                s_Schedule.Id = req.JobKey;
#pragma warning restore CS8601 // Possible null reference assignment.
                var bodyReq = JsonConvert.SerializeObject(s_Schedule);

                _commonFunction.CallApiStopJobOnRingTenant(bodyReq, authorization);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"PauseJobSchedule error because: {ex}");
                return false;
            }
        }

        public bool StopJobScheduleRoot(HttpRequest requestContext, ReqJobSchedule req)
        {
            try
            {
                var authorize = requestContext.Headers["Authorization"].ToString();
                S_ScheduleShrinkLog s_ScheduleShrinkLog = new S_ScheduleShrinkLog();
                s_ScheduleShrinkLog.Id = req?.JobKey ?? "";
                var bodyReq = JsonConvert.SerializeObject(s_ScheduleShrinkLog);
                _commonFunction.CallApiStopJobOnRingRoot(bodyReq, authorize);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Stop job schedule an error has occurred: {ex}");
                return false;
            }
        }

        public bool StopJobScheduleOnRingRoot(ReqJobSchedule req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.JobKey))
                {
                    Log.Error($"Stop job fail because JobKey is null or empty.");
                    return false;
                }
                _commonFunction.DeleteJobInSchedule(req.JobKey, req.JobKey.Replace("Trigg", "Job"));

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Stop job an error has occurred: {ex}");
                return false;
            }
        }

        public bool StartJobScheduleRoot(HttpRequest requestContext, ReqJobSchedule req)
        {
            try
            {
                var bodyReq = JsonConvert.SerializeObject(req);
                var authorize = requestContext.Headers["Authorization"].ToString();
                _commonFunction.CallApiStartJobOnRingRoot(bodyReq, authorize);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Start job schedule an error has occurred: {ex}");

                return false;
            }
        }

        public bool StartJobScheduleTenant(HttpRequest requestContext, ReqJobSchedule req)
        {
            try
            {
                var authorization = requestContext.Headers["Authorization"].ToString();
                var bodyReq = JsonConvert.SerializeObject(req);
                _commonFunction.CallApiStartJobOnRingTenant(bodyReq, authorization);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"ResumJobSchedule error because: {ex}");
                return false;
            }
        }

        public List<ResScheduleInSite> GetAllJobRunningTenant(HttpRequest requestContext)
        {
            return _commonFunction.GetAllJobRunning(requestContext, IBGlobalConfig.ScheduleServiceIBox.ToList());
        }

        public List<ResScheduleInSite> GetAllJobRunningRoot(HttpRequest requestContext)
        {
            return _commonFunction.GetAllJobRunning(requestContext, IBGlobalConfig.RootServersIBox.ToList());
        }

        public ResScheduleInSite GetAllJobRunningOnRing(string? tenantId = "")
        {
            try
            {
                return _commonFunction.GetAllJobRunningOnRing(tenantId);
            }
            catch (Exception ex)
            {
                throw new IboxLog($"GetAllJobRunningOnRing error: {ex}", tenantId);
            }
        }

        public void RunScheduleTenant()
        {
            try
            {
                string thisSite = IBGlobalConfig.ThisSite ?? string.Empty;
                List<ResScheduleInSite> resJobSchedule = GetJobSchedulesFromOtherSites(thisSite);

                foreach (var tenant in IBGlobalConfig.Tenants)
                {
                    using (var tenantContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption)))
                    {
                        var tenantDbContext = tenantContext.GetTenantContext(tenant.Id).Context;

                        var s_Schedules = tenantDbContext.S_Schedules
                            .Where(ptr => !ptr.IsDelete && ptr.CreatedDate != null && ptr.Site == thisSite)
                            .OrderByDescending(ptr => ptr.CreatedDate)
                            .ToList();

                        DeleteInvalidJobs(resJobSchedule, s_Schedules);

                        CreateJobsForSchedules(tenant.Id, s_Schedules);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RunScheduleTenant: {ex.Message}");
            }
        }

        public void RemoveFileUnZip(string folderPath)
        {
            try
            {
                TimeSpan ageThreshold = TimeSpan.FromMinutes(30);
                if (!Directory.Exists(folderPath))
                {
                    return;
                }

                string[] allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                foreach (string file in allFiles)
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(file);
                    TimeSpan fileAge = DateTime.Now - lastWriteTime;

                    if (fileAge > ageThreshold)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception)
                        {
                            Log.Error($"Delete File: {file}");
                        }
                    }
                }

                string[] allFolders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);
                foreach (string folder in allFolders)
                {
                    try
                    {
                        DateTime lastWriteTime = Directory.GetLastWriteTime(folder);
                        TimeSpan fileAge = DateTime.Now - lastWriteTime;

                        if (fileAge > ageThreshold)
                        {
                            Directory.Delete(folder, recursive: true);
                        }
                    }
                    catch (Exception)
                    {
                        Log.Error($"Delete folder: {folder}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RemoveFileUnZip: {ex.Message}");
            }
        }

        private List<ResScheduleInSite> GetJobSchedulesFromOtherSites(string thisSite)
        {
            var resJobSchedule = new List<ResScheduleInSite>();
            var urlScheduleServiceIBoxs = IBGlobalConfig.ScheduleServiceIBox.Where(ptr => !ptr.Contains(thisSite));

            foreach (var UrlscheduleService in urlScheduleServiceIBoxs)
            {
                string urlScheduleService = $"{UrlscheduleService}{UrlServiceIBConfig.UrlGetAllJob}";
                var result = _restAPI.Send(new RestAPIRequest
                {
                    Headers = new List<RestAPIHeader>
                        {
                            new RestAPIHeader ()
                            {
                                Label = "Content-Type",
                                Value = "application/json"
                            }
                        },
                    Method = "GET",
                    Timeout = 30,
                    Url = urlScheduleService
                }, "AppLogs");

                if (result?.Result != null)
                {
                    var data = JsonConvert.DeserializeObject<ResponseForm<ResScheduleInSite>>(result.Result);
                    if (data?.Data != null)
                    {
                        resJobSchedule.Add(data.Data);
                    }
                }
            }

            return resJobSchedule;
        }

        private List<ResScheduleInSite> GetJobSchedulesFromOtherSitesRoot(string thisSite)
        {
            var resJobSchedule = new List<ResScheduleInSite>();
            var urlRootServiceIBoxs = IBGlobalConfig.RootServersIBox.Where(ptr => !ptr.Contains(thisSite));

            foreach (var UrlscheduleService in urlRootServiceIBoxs)
            {
                string urlScheduleService = $"{UrlscheduleService}{UrlServiceIBConfig.UrlGetAllJobRoot}";
                var result = _restAPI.Send(new RestAPIRequest
                {
                    Headers = new List<RestAPIHeader>
                        {
                            new RestAPIHeader { Label = "Content-Type", Value = "application/json" }
                        },
                    Method = "GET",
                    Timeout = 30,
                    Url = urlScheduleService
                }, "AppLogs");

                if (result?.Result != null)
                {
                    var data = JsonConvert.DeserializeObject<ResponseForm<ResScheduleInSite>>(result.Result);
                    if (data?.Data != null)
                    {
                        resJobSchedule.Add(data.Data);
                    }
                }
            }

            return resJobSchedule;
        }

        private void DeleteInvalidJobs(List<ResScheduleInSite> resJobSchedule, List<S_Schedule> s_Schedules)
        {
            try
            {
                var urlScheduleServiceDeleteJobTemplate = UrlServiceIBConfig.UrlDeleteJobSchedule;

                foreach (var job in resJobSchedule)
                {

                    var filteredSchedules = job?.JobSchedules?
                        .Where(ptr => s_Schedules.Any(schedule => ptr.Trigger.Name.Contains(schedule.Id.ToString())))
                        .ToList();


                    if (filteredSchedules != null && filteredSchedules.Count > 0)
                    {
                        var urlScheduleServiceSite = IBGlobalConfig.ScheduleServiceIBox
                            .FirstOrDefault(ptr => ptr.Contains(job?.Site ?? ""));

                        foreach (var schedule in filteredSchedules)
                        {
                            var urlScheduleServiceDeleteJob = $"{urlScheduleServiceSite}{urlScheduleServiceDeleteJobTemplate}";
                            _restAPI.Send(new RestAPIRequest
                            {
                                Body = JsonConvert.SerializeObject(new S_Schedule { Id = schedule?.Trigger?.Name ?? "" }),
                                Headers = new List<RestAPIHeader>
                                    {
                                        new RestAPIHeader { Label = "Content-Type", Value = "application/json" }
                                    },
                                Method = "POST",
                                Timeout = 30,
                                Url = urlScheduleServiceDeleteJob
                            }, "AppLogs");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteInvalidJobs: {ex.Message}\n List<ResScheduleInSite> :{JsonConvert.SerializeObject(resJobSchedule)}, List<S_Schedule> {JsonConvert.SerializeObject(s_Schedules)}");
            }
        }

        private void DeleteInvalidJobsRoot(List<ResScheduleInSite> resJobSchedule, List<S_ScheduleShrinkLog> s_ScheduleShrinkLogs)
        {
            var urlRootServiceDeleteJobTemplate = UrlServiceIBConfig.UrlDeleteJobScheduleRoot;

            foreach (var job in resJobSchedule)
            {

                var filteredSchedules = job?.JobSchedules?
                    .Where(ptr => s_ScheduleShrinkLogs.Any(schedule => ptr.Trigger.Name.Contains(schedule.Id.ToString())))
                    .ToList();


                if (filteredSchedules != null && filteredSchedules.Count > 0)
                {
                    var urlRootServiceSite = IBGlobalConfig.RootServersIBox
                        .FirstOrDefault(ptr => ptr.Contains(job?.Site ?? ""));

                    foreach (var schedule in filteredSchedules)
                    {
                        var urlRootServiceDeleteJobRoot = $"{urlRootServiceSite}{urlRootServiceDeleteJobTemplate}";
                        _restAPI.Send(new RestAPIRequest
                        {
                            Body = JsonConvert.SerializeObject(new S_Schedule { Id = schedule?.Trigger?.Name ?? "" }),
                            Headers = new List<RestAPIHeader>
                                    {
                                        new RestAPIHeader { Label = "Content-Type", Value = "application/json" }
                                    },
                            Method = "POST",
                            Timeout = 30,
                            Url = urlRootServiceDeleteJobRoot
                        }, "AppLogs");
                    }
                }
            }
        }

        private void CreateJobsForSchedules(string tenantId, List<S_Schedule> s_Schedules)
        {
            try
            {
                foreach (var s_Schedule in s_Schedules)
                {
                    var scheduleBase = _commonFunction.ConvertToScheduleBase(s_Schedule);
                    var cronExpression = _commonFunction.GetCronExpression(scheduleBase);

                    var reqCreateJob = new ReqCreateScheduleJob
                    {
                        WfId = s_Schedule.Wfid,
                        ScheduleId = s_Schedule.Id,
                        TenantId = tenantId,
                        Site = s_Schedule.Site,
                        CronExpression = cronExpression,
                        TypeSchedule = s_Schedule.Type,
                        JobName = s_Schedule.Name
                    };

                    _commonFunction.CreateScheduleJobTenant(reqCreateJob);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateJobsForSchedules: {ex.Message} \n tenantId: {tenantId}, List<S_Schedule> {JsonConvert.SerializeObject(s_Schedules)}");
            }
        }

        private void CreateJobsForSchedulesRoot(List<S_ScheduleShrinkLog> s_SchedulesShrinkLog)
        {
            try
            {
                foreach (var scheduleShrinkLog in s_SchedulesShrinkLog)
                {
                    var scheduleBase = _commonFunction.ConvertToScheduleBase(scheduleShrinkLog);
                    var cronExpression = _commonFunction.GetCronExpression(scheduleBase);

                    var reqCreateJob = new ReqCreateScheduleJob()
                    {
                        ScheduleId = scheduleShrinkLog.Id,
                        ListCategory = scheduleShrinkLog.ListCategory,
                        Site = scheduleShrinkLog.Site,
                        CronExpression = cronExpression,
                        MethodShrinkLog = scheduleShrinkLog.MethodShrinkLog,
                        TypeSchedule = (Database.Tenant.Tables.TypeScheduleBase?)scheduleShrinkLog.Type,
                        JobName = scheduleShrinkLog.Name
                    };

                    _commonFunction.CreateScheduleJobRoot(reqCreateJob);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateJobsForSchedulesRoot: {ex.Message}");
                throw;
            }
        }

        public void RunScheduleRoot()
        {
            try
            {
                string thisSite = IBGlobalConfig.ThisSite ?? string.Empty;

                List<ResScheduleInSite> resJobSchedule = GetJobSchedulesFromOtherSitesRoot(thisSite);
                foreach (var item in IBGlobalConfig.RootServersIBox)
                {
                    var rootContext = new RootContext(_configuration, _encryption).Context;
                    var schedulesShrinkLog = rootContext
                        .S_ScheduleShrinkLogs
                        .Where(ptr => !ptr.IsDelete && ptr.Site == thisSite).ToList();

                    DeleteInvalidJobsRoot(resJobSchedule, schedulesShrinkLog);

                    CreateJobsForSchedulesRoot(schedulesShrinkLog);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RunScheduleRoot: {ex.Message}");
            }
        }

        public bool DeleteJobSchedule(object req)
        {
            try
            {

                var schedule = JsonConvert.DeserializeObject<S_Schedule>(req.ToString());

                if (schedule == null || string.IsNullOrEmpty(schedule.Id))
                {
                    return false;
                }
                var jobGroups = _scheduler.GetJobGroupNames().Result;

                foreach (string group in jobGroups)
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                    var jobKeys = _scheduler.GetJobKeys(groupMatcher).Result;

                    foreach (var jobKey in jobKeys)
                    {
                        var triggers = _scheduler.GetTriggersOfJob(jobKey).Result;

                        if (triggers.Any(trigger => trigger.Key.ToString().Contains(schedule.Id)))
                        {
                            _scheduler.DeleteJob(jobKey);
                            return true;
                        }
                        else if (triggers.Any(trigger => trigger.JobKey.Name.ToString().Contains(schedule.Id)))
                        {
                            _scheduler.DeleteJob(jobKey);
                            return true;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteJobSchedule: {ex.Message}");
                return false;
            }
        }

        public bool DeleteJobScheduleRoot(object req)
        {
            try
            {

                var scheduleShrinkLog = JsonConvert.DeserializeObject<S_ScheduleShrinkLog>(req.ToString());

                if (scheduleShrinkLog == null || string.IsNullOrEmpty(scheduleShrinkLog.Id))
                {
                    return false;
                }
                var jobGroups = _scheduler.GetJobGroupNames().Result;

                foreach (string group in jobGroups)
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                    var jobKeys = _scheduler.GetJobKeys(groupMatcher).Result;

                    foreach (var jobKey in jobKeys)
                    {
                        var triggers = _scheduler.GetTriggersOfJob(jobKey).Result;

                        if (triggers.Any(trigger => trigger.Key.ToString().Contains(scheduleShrinkLog.Id)))
                        {
                            _scheduler.DeleteJob(jobKey);
                            return true;
                        }
                        else if (triggers.Any(trigger => trigger.JobKey.Name.ToString().Contains(scheduleShrinkLog.Id)))
                        {
                            _scheduler.DeleteJob(jobKey);
                            return true;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteJobSchedule: {ex.Message}");
                return false;
            }
        }
    }
}