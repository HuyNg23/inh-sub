using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.SelfService.HubScheduler;
using IBox.Schedule.SelfService.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace IBox.Schedule.SelfService.HandleJobSchedule
{
    public class HandleJobSchedules : IHandleJobSchedules
    {
        private readonly IScheduler _scheduler;
        private readonly IBContext<TenantContext> _tenantContext;
        private readonly IConfiguration _configuration;
        private readonly IHubSchedule _hubSchedule;
        public HandleJobSchedules(IScheduler scheduler, IBContext<TenantContext> tenantContext, IConfiguration configuration, IHubSchedule hubSchedule)
        {
            this._scheduler = scheduler;
            this._tenantContext = tenantContext;
            this._configuration = configuration;
            this._hubSchedule = hubSchedule;
        }

        /// <summary>
        /// Start công việc ngay lập tức
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        public bool StartJobNow(HttpRequest requestContext, RequestForm<S_Schedule> requestJobSchedule)
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Start job now in site {thisSite}.");

                var authorization = requestContext.Headers["Authorization"].ToString();
                var tenantID = requestContext.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(tenantID))
                {
                    throw new IboxException($"Tenant can't null or empty.");
                }

                if (string.IsNullOrEmpty(requestJobSchedule?.Body?.Site?.Trim()))
                {
                    throw new IboxException($"Site can't null or empty.");
                }

                if (string.IsNullOrEmpty(requestJobSchedule?.Body?.Wfid?.Trim()))
                {
                    throw new IboxException($"WF id can't null or empty.");
                }

                var tenantContext = _tenantContext.GetTenantContext(tenantID).Context;

                var schedule = tenantContext.Context.S_Schedules.FirstOrDefault(ptr =>
                                                                                !ptr.IsDelete
                                                                                && ptr.Site == requestJobSchedule.Body.Site
                                                                                && ptr.Id == requestJobSchedule.Body.Id);

                if (schedule == null)
                {
                    Log.Error("Start job now fail because schedule is null.");

                    throw new IboxException($"Schedule is null.");
                }

                var planSchedule = tenantContext.Context.S_ImplementationPlans.FirstOrDefault(ptr =>
                                                                                            ptr.ScheduleID == schedule.Id
                                                                                            && !ptr.IsDelete
                                                                                            && ptr.CreatedDate != null
                                                                                            && DateTime.Now.Date == ptr.CreatedDate.Value.Date);

                if (planSchedule != null)
                {
                    planSchedule.TryUpdate(tenantContext.Context, new S_ImplementationPlan
                    {
                        Id = planSchedule.Id,
                        IsDelete = true
                    });
                }

                MakePlanForTenant(schedule, tenantContext);

                var listServerSite = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == TypeService.ScheduleService).ToList();

                var _serverSite = listServerSite.Find(x => x.Site == requestJobSchedule.Body.Site);

                if (_serverSite == null)
                {
                    Log.Error($"Site {requestJobSchedule.Body.Site} is not found in list server site.");

                    throw new IboxException($"{requestJobSchedule.Body.Site} is not in server site list");
                }

                if (planSchedule != null && !string.IsNullOrEmpty(planSchedule.KeyTrigg))
                {
                    var requestStopSchedule = new RequestJobSchedule()
                    {
                        JobKey = planSchedule.KeyTrigg
                    };

                    var bodyStopSchedule = JsonConvert.SerializeObject(requestStopSchedule);

                    CallApiStopJobScheduleOnRing(bodyStopSchedule, authorization);
                }

                Thread.Sleep(1500);

                var bodyReq = JsonConvert.SerializeObject(requestJobSchedule);

                if (_serverSite.Site == thisSite)
                {
                    CallApiRunSpecifiedJob(_serverSite, authorization, bodyReq);

                    return true;
                }

                string urlHub = $"{BuildUrlHub(_serverSite)}/ChatHubSchedule";

                bool isReachable = this._hubSchedule.CheckHubReconnected(urlHub);

                if (isReachable)
                {
                    CallApiRunSpecifiedJob(_serverSite, authorization, bodyReq);
                }
                else
                {
                    Log.Information("Hub is not reachable.", urlHub);

                    foreach (var serverSite in listServerSite)
                    {
                        string urlHubRun = $"{BuildUrlHub(serverSite)}/ChatHubSchedule";

                        bool isCheckSiteRun = this._hubSchedule.CheckHubReconnected(urlHubRun);

                        if (isCheckSiteRun)
                        {
                            CallApiRunSpecifiedJob(serverSite, authorization, bodyReq);

                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex,"Start_Job_Now error because");
                return false;
            }
        }

        /// <summary>
        /// Call api stop job onRing
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        private void CallApiStopJobScheduleOnRing(string bodyReq, string authorization)
        {
            StringContent bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.ScheduleServiceIBox.ToList().ForEach(ip =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        var rest = string.Format(@"{0}{1}", ip, "/api/Job/StopJobScheduleOnRing");
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                        requestMessage.Headers.Add("Authorization", authorization);
                        requestMessage.Content = bodyReqStopSchedule;
                        httpClient.SendAsync(requestMessage).Wait();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Call api stop job schedule onRing in site {ip} fail because: {ex}");
                    }
                });
            });
        }

        /// <summary>
        /// Call api RunSpecifiedJob
        /// </summary>
        /// <param name="service"></param>
        /// <param name="authorization"></param>
        /// <param name="bodyReq"></param>
        private void CallApiRunSpecifiedJob(S_Service service, string authorization, string bodyReq)
        {
            var site = IBGlobalConfig.ScheduleServiceIBox.Find(x => x.Contains(service.Site ?? ""));
            StringContent dataRequest = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            Task.Run(() =>
            {
                try
                {
                    var rest = string.Format(@"{0}{1}", site, "/api/Job/RunSpecifiedJob");
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    HttpClient httpClient = new HttpClient(handler);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                    requestMessage.Headers.Add("Authorization", authorization);
                    requestMessage.Content = dataRequest;
                    httpClient.SendAsync(requestMessage).Wait();
                }
                catch (Exception ex)
                {
                    Log.Error($"Call api RunSpecifiedJob in site {site} error because: {ex}");
                }
            });
        }

        /// <summary>
        /// Cấu hình plan schedule
        /// </summary>
        /// <param name="schedule"></param>
        /// <exception cref="IboxException"></exception>
        private void MakePlanForTenant(S_Schedule schedule, TenantContext tenantContext)
        {
            switch (schedule.Type)
            {
                case Database.Tenant.Tables.TypeScheduleBase.NumberOfHoursMinutes:
                    if (schedule.Hour == "00")
                    {
                        #region Mỗi ...phút một lần
                        EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                        {
                            Year = "*",
                            Month = "*",
                            Day = "*",
                            Hour = "*",
                            Minute = schedule.Minute,
                            Second = "0",
                            Weekday = string.Empty,
                            Name = schedule.Name,
                            Wfid = schedule.Wfid,
                            StatusPlan = StatusPlan.Await,
                            ScheduleID = schedule.Id,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            Type = schedule.Type,
                            FormatCron = $"0 0/{FormartTime(schedule.Minute)} * * * ?"
                        });
                        #endregion
                    }
                    else if (schedule.Minute == "00")
                    {
                        EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                        {
                            Year = "*",
                            Month = "*",
                            Day = "*",
                            Hour = schedule.Hour,
                            Minute = "00",
                            Second = "0",
                            Weekday = string.Empty,
                            Name = schedule.Name,
                            Wfid = schedule.Wfid,
                            StatusPlan = StatusPlan.Await,
                            ScheduleID = schedule.Id,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            Type = schedule.Type,
                            FormatCron = $"0 0 */{FormartTime(schedule.Hour)} ? * *"
                        });
                    }
                    else
                    {
                        #region Mỗi giờ(hai hoặc ba giờ,...) phút thứ... một lần
                        EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                        {
                            Year = "*",
                            Month = "*",
                            Day = "*",
                            Hour = schedule.Hour,
                            Minute = schedule.Minute,
                            Second = "0",
                            Weekday = string.Empty,
                            Name = schedule.Name,
                            Wfid = schedule.Wfid,
                            StatusPlan = StatusPlan.Await,
                            ScheduleID = schedule.Id,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            Type = schedule.Type,
                            FormatCron = $"0 {FormartTime(schedule.Minute)} */{FormartTime(schedule.Hour)} ? * *"
                        });
                        #endregion
                    }
                    break;
                case Database.Tenant.Tables.TypeScheduleBase.Daily:
                    #region Hàng ngày vào giờ... phút...
                    EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                    {
                        Year = "*",
                        Month = "*",
                        Day = "*",
                        Hour = schedule.Hour,
                        Minute = schedule.Minute,
                        Second = "0",
                        Weekday = string.Empty,
                        Name = schedule.Name,
                        Wfid = schedule.Wfid,
                        ScheduleID = schedule.Id,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        StatusPlan = StatusPlan.Await,
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        Type = schedule.Type,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} * * ? *"
                    });
                    break;
                #endregion
                case Database.Tenant.Tables.TypeScheduleBase.Weekly:
                    #region Hàng tuần vào thứ... giờ... phút...
                    EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                    {
                        Year = "*",
                        Month = "*",
                        Day = "*",
                        Hour = schedule.Hour,
                        Minute = schedule.Minute,
                        Second = "0",
                        Weekday = schedule.Weekday,
                        Name = schedule.Name,
                        Wfid = schedule.Wfid,
                        StatusPlan = StatusPlan.Await,
                        ScheduleID = schedule.Id,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        Type = schedule.Type,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} ? * {schedule.Weekday}"
                    });
                    break;
                #endregion
                case Database.Tenant.Tables.TypeScheduleBase.Monthly:
                    #region Hàng tháng vào ngày... giờ... phút...
                    EntityAction.TryCreate<S_ImplementationPlan>(null, tenantContext, new S_ImplementationPlan()
                    {
                        Year = "*",
                        Month = "*",
                        Day = schedule.Day,
                        Hour = schedule.Hour,
                        Minute = schedule.Minute,
                        Second = "0",
                        Weekday = string.Empty,
                        Name = schedule.Name,
                        Wfid = schedule.Wfid,
                        StatusPlan = StatusPlan.Await,
                        ScheduleID = schedule.Id,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        Type = schedule.Type,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} {schedule.Day} * ?"
                    });
                    break;
                #endregion
                default:
                    Log.Error($"Not found type schedule {schedule.Id}");
                    throw new IboxException($"Not found type {schedule.Type} schedule {schedule.Id}");
            }
        }

        /// <summary>
        /// Dừng công việc đang chạy
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        public bool StopJobSchedule(HttpRequest requestContext, RequestJobSchedule requestJobSchedule)
        {
            try
            {
                var authorization = requestContext.Headers["Authorization"].ToString();
                var bodyReq = JsonConvert.SerializeObject(requestJobSchedule);

                CallApiStopJobScheduleOnRing(bodyReq, authorization);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"StopJobSchedule error because: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Dừng công việc đang chạy trên site
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        public bool StopJobScheduleOnRing(string tenantId, RequestJobSchedule requestJobSchedule)
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Stop_Job: Begin stop job {requestJobSchedule.JobKey} in site {thisSite}");

                if (string.IsNullOrEmpty(requestJobSchedule.JobKey))
                {
                    Log.Error($"Stop_Job fail: JobKey is null or empty.");
                    return false;
                }

                var tennantContext = this._tenantContext.GetTenantContext(tenantId).Context;

                var implementationPlan = tennantContext
                    .S_ImplementationPlans
                    .Where(ptr => ptr.KeyTrigg == requestJobSchedule.JobKey && !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date)
                    .FirstOrDefault();

                if (implementationPlan == null)
                {
                    StopSchedule(requestJobSchedule.JobKey, thisSite);
                    return false;
                }

                var jobKeyStop = new JobKey($"{requestJobSchedule.JobKey}", "Group2");

                if (!this._scheduler.IsStarted)
                {
                    this._scheduler.Start();
                    Thread.Sleep(1000);
                }

                if (this._scheduler.IsStarted)
                {
                    bool jobIsExists = this._scheduler.CheckExists(jobKeyStop).Result;

                    if (jobIsExists)
                    {
                        this._scheduler.DeleteJob(jobKeyStop);

                        Log.Information($"Stop_Job: Stop job {requestJobSchedule.JobKey} in site {thisSite} success.");

                        implementationPlan.TryUpdate(tennantContext, new S_ImplementationPlan()
                        {
                            Id = implementationPlan.Id,
                            StatusPlan = StatusPlan.Stop
                        });

                        return true;
                    }
                    else
                    {
                        implementationPlan.TryUpdate(tennantContext, new S_ImplementationPlan()
                        {
                            Id = implementationPlan.Id,
                            StatusPlan = StatusPlan.Stop
                        });

                        Log.Error($"Stop_Job fail: Because job {requestJobSchedule.JobKey} is not exists in site {thisSite}.");

                        return false;
                    }
                }
                else
                {
                    Log.Error($"Stop_Job fail: Because scheduler is not start in site {thisSite}.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Stop_Job error: Because {ex}");
                return false;
            }
        }

        private void StopSchedule(string jobKey, string thisSite)
        {
            var jobKeyStop = new JobKey($"{jobKey}", "Group2");

            if (!this._scheduler.IsStarted)
            {
                this._scheduler.Start();
                Thread.Sleep(1000);
            }

            if (this._scheduler.IsStarted)
            {
                bool jobIsExists = this._scheduler.CheckExists(jobKeyStop).Result;

                if (jobIsExists)
                {
                    this._scheduler.DeleteJob(jobKeyStop);

                    Log.Information($"Stop_Job: Stop job {jobKey} in site {thisSite} success.");
                }
                else
                {
                    Log.Error($"Stop_Job fail: Because job {jobKey} is not exist in site {thisSite}.");
                }
            }
            else
            {
                Log.Error($"Stop_Job fail: Because scheduler is not start in site {thisSite}.");
            }
        }

        /// <summary>
        /// Khởi động lại công việc đã tắt
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public bool StartJobSchedule(HttpRequest requestContext, RequestJobSchedule req)
        {
            try
            {
                var Authorize = requestContext.Headers["Authorization"];
                var data = JsonConvert.SerializeObject(req);
                StringContent dataRequest = new StringContent(data, Encoding.UTF8, "application/json");

                IBGlobalConfig.ScheduleServiceIBox.ToList().ForEach(ip =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            try
                            {
                                var rest = string.Format(@"{0}{1}", ip, "/api/Job/StartJobScheduleOnRing");
                                var handler = new HttpClientHandler();
                                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                HttpClient httpClient = new HttpClient(handler);
                                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                                requestMessage.Headers.Add("Authorization", Authorize.ToString());
                                requestMessage.Content = dataRequest;
                                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, ex.Message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Can't start job to Server: {ip} \n With Error {ex.Message}");
                        }
                    });
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"StartJobSchedule error: {ex}");
                throw new IboxException($"Can not start job: {ex.Message}");
            }
        }

        /// <summary>
        /// Khởi động lại công việc đã tắt trên site
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        public bool StartJobScheduleOnRing(string tenantId, RequestJobSchedule requestJobSchedule)
        {
            try
            {
                string thisSite = this._configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Start_Job: Begin start job: {requestJobSchedule.JobKey} in site: {thisSite}.");

                if (string.IsNullOrEmpty(requestJobSchedule.JobKey))
                {
                    Log.Error("$Start_Job fail: Because JobKey is null or empty.");
                    return false;
                }

                if (string.IsNullOrEmpty(thisSite))
                {
                    Log.Error("$Start_Job fail: Because not found this site to run.");
                    return false;
                }

                var tennantContext = this._tenantContext.GetTenantContext(tenantId).Context;

                var implementationPlan = tennantContext
                    .S_ImplementationPlans
                    .Where(ptr => ptr.KeyTrigg == requestJobSchedule.JobKey && !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date)
                    .FirstOrDefault();

                if (implementationPlan == null)
                {
                    Log.Error($"Start_Job fail: Because not found keyJob {requestJobSchedule.JobKey}.");
                    return false;
                }

                if (string.IsNullOrEmpty(implementationPlan.FormatCron))
                {
                    Log.Error("Start_Job fail: Because formatCron of job is null or empty.");
                    return false;
                }

                if (implementationPlan.SiteRunning != thisSite)
                {
                    Log.Error($"Start_Job fail: Because Job {requestJobSchedule.JobKey} not match to run in site: {thisSite}.");
                    return false;
                }

                var jobKeyNew = new JobKey($"{requestJobSchedule.JobKey}", "Group2");

                var jobTriggerNew = new TriggerKey($"{requestJobSchedule.JobKey.Replace("Job", "Trigg")}", "Group2");

                bool isJobExists = this._scheduler.CheckExists(jobKeyNew).Result;

                if (!this._scheduler.IsStarted)
                {
                    this._scheduler.Start();
                    Thread.Sleep(1000);
                }

                if (this._scheduler.IsStarted)
                {
                    if (isJobExists)
                    {
                        this._scheduler.UnscheduleJob(jobTriggerNew);
                        this._scheduler.DeleteJob(jobKeyNew);
                    }

                    var job = JobBuilder.Create<ExcuteWorkflow>()
                                    .UsingJobData("wfid", implementationPlan.Wfid)
                                    .UsingJobData("planId", implementationPlan.Id)
                                    .UsingJobData("scheduleId", implementationPlan.ScheduleID)
                                    .UsingJobData("tenantId", tenantId)
                                    .UsingJobData("typeSchedule", ((int?)implementationPlan.Type).ToString())
                                    .UsingJobData("jobKey", jobKeyNew.Name)
                                    .UsingJobData("isDeploy", "False")
                                    .WithIdentity(jobKeyNew)
                                    .Build();

                    ITrigger trigger = TriggerBuilder.Create()
                        .WithIdentity(jobTriggerNew)
                        .WithCronSchedule(implementationPlan.FormatCron)
                        .Build();

                    this._scheduler.ScheduleJob(job, trigger);

                    Log.Information($"Start_Job: Start job {requestJobSchedule.JobKey} in site {thisSite} success.");

                    implementationPlan.TryUpdate(tennantContext, new S_ImplementationPlan()
                    {
                        Id = implementationPlan.Id,
                        StatusPlan = StatusPlan.Complete
                    });
                }
                else
                {
                    Log.Error($"Start_Job fail: Because scheduler is not start in site {thisSite}.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Start_Job error: Because {ex}");
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách Job đang chạy hoặc đã stop trên schedule trong ngày
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="requestAllScheduleHistory"></param>
        /// <returns></returns>
        public ResponseAllImplementationPlans AllImplementationPlansDuringDay(string tenantID, RequestAllImplementationPlansDuringDay requestAllScheduleHistory)
        {
            try
            {
                var tenantContext = this._tenantContext.GetTenantContext(tenantID).Context;

                var implementationPlans = tenantContext.S_ImplementationPlans
                    .Where(ptr => !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date)
                    .OrderByDescending(ptr => ptr.CreatedDate);

                if (requestAllScheduleHistory.PageNum == -1)
                {
                    return new ResponseAllImplementationPlans()
                    {
                        Data = implementationPlans.ToList(),
                        TotalReCords = implementationPlans.Count()
                    };
                }
                else
                {
                    int numberOfObjectsPerPage = 30;
                    var queryPage = implementationPlans
                        .Skip(numberOfObjectsPerPage * (requestAllScheduleHistory.PageNum - 1))
                        .Take(numberOfObjectsPerPage)
                        .ToList();

                    return new ResponseAllImplementationPlans()
                    {
                        Data = queryPage,
                        TotalReCords = implementationPlans.Count()
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Error($"AllImplementationPlansDuringDay error: {ex}");
                throw new IboxException(ex.Message);
            }
        }

        /// <summary>
        /// Chạy ngay job được chỉ định
        /// </summary>
        /// <param name="ternantId"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public bool RunSpecifiedJob(string ternantId, RequestForm<S_Schedule> req)
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? "";

                Log.Information($"RunSpecifiedJob: start job now in site {thisSite}.");

                using (var tenantCon = _tenantContext.GetTenantContext(ternantId).Context)
                {
                    var planSchedule = tenantCon.S_ImplementationPlans.FirstOrDefault(ptr => !ptr.IsDelete && ptr.KeyTrigg == $"Job_{req.Body.Id}_{req.Body.Site}" && ptr.CreatedDate != null && DateTime.Now.Date == ptr.CreatedDate.Value.Date);

                    if (planSchedule == null)
                    {
                        Log.Error($"RunSpecifiedJob error: Because not found plan in site {thisSite}.");
                        throw new IboxException("Not found plan.");
                    }

                    try
                    {
                        if (!this._scheduler.IsStarted)
                        {
                            this._scheduler.Start();
                            Thread.Sleep(1000);
                        }

                        if (this._scheduler.IsStarted)
                        {
                            var jobKey = new JobKey($"Job_{planSchedule.ScheduleID}_{planSchedule.Site}", "Group2");
                            var jobTrigger = new TriggerKey($"Trigg_{planSchedule.ScheduleID}_{planSchedule.Site}", "Group2");

                            bool isJobExists = this._scheduler.CheckExists(jobKey).Result;

                            if (isJobExists)
                            {
                                Log.Information($"RunSpecifiedJob: Delete Job_{planSchedule.ScheduleID}_{planSchedule.Site} in site {thisSite}.");

                                this._scheduler.UnscheduleJob(jobTrigger);

                                this._scheduler.DeleteJob(jobKey);
                            }

                            string ConvertCron = !string.IsNullOrEmpty(planSchedule.FormatCron) ? planSchedule.FormatCron : $"{planSchedule.Second} {planSchedule.Minute} {planSchedule.Hour} ? {planSchedule.Month} {planSchedule.Day} {planSchedule.Year}";

                            var job = JobBuilder.Create<ExcuteWorkflow>()
                                .UsingJobData("wfid", planSchedule.Wfid)
                                .UsingJobData("planId", planSchedule.Id)
                                .UsingJobData("scheduleId", planSchedule.ScheduleID)
                                .UsingJobData("tenantId", ternantId)
                                .UsingJobData("typeSchedule", ((int?)planSchedule.Type).ToString())
                                .UsingJobData("jobKey", jobKey.Name)
                                .UsingJobData("isDeploy", "True")
                                .WithIdentity(jobKey)
                                .Build();

                            ITrigger trigger = TriggerBuilder.Create()
                                .WithIdentity(jobTrigger)
                                .WithCronSchedule(ConvertCron)
                                .Build();

                            this._scheduler.ScheduleJob(job, trigger);

                            Log.Information($"RunSpecifiedJob: Start job Job_{planSchedule.ScheduleID}_{planSchedule.Site} in site: {thisSite} success.");

                            tenantCon.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == planSchedule.Id).TryUpdate(tenantCon, new S_ImplementationPlan()
                            {
                                Id = planSchedule.Id,
                                StatusPlan = StatusPlan.Complete,
                                SiteRunning = thisSite
                            });
                        }
                        else
                        {
                            Log.Error($"RunSpecifiedJob error: Because scheduler is not start in site {thisSite}.");
                            throw new IboxException("Scheduler is not start.");
                        }
                    }
                    catch (Exception ex)
                    {
                        tenantCon.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == planSchedule.Id).TryUpdate(tenantCon, new S_ImplementationPlan()
                        {
                            Id = planSchedule.Id,
                            StatusPlan = StatusPlan.Fail,
                        });

                        Log.Error("RunSpecifiedJob error: itemPlanScheduler: " + JsonConvert.SerializeObject(planSchedule) + $"\n RunJobDay_Exception: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RunSpecifiedJob error: {ex}");
                throw new IboxException($"RunSpecifiedJob: {ex}");
            }

            return true;
        }

        private string FormartTime(string time)
        {
            if (time.StartsWith('0'))
            {
                return time.Remove(0, 1);
            }

            return time;
        }

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên các site
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public List<ScheduleInSite> GetAllJobRunning(HttpRequest requestContext)
        {
            try
            {
                List<ScheduleInSite> result = new List<ScheduleInSite>();
                var Authorize = requestContext.Headers["Authorization"];
                IBGlobalConfig.ScheduleServiceIBox.ToList().ForEach(ip =>
                {
                    try
                    {
                        var rest = string.Format(@"{0}{1}", ip, "/api/Job/GetAllJobRunningOnRing");
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                        requestMessage.Headers.Add("Authorization", Authorize.ToString());
                        HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;

                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var responseContent = httpResponse.Content.ReadAsStringAsync().Result;

                            var data = JsonConvert.DeserializeObject<ResponseForm<ScheduleInSite>>(responseContent);

                            if (data != null)
                            {
                                if (data.Data != null)
                                {
                                    result.Add(data.Data);
                                }
                            }
                            else
                            {
                                Log.Error("GetAllJobRunning: data GetAllJobRunningOnRing is null.");
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
                throw new IboxException($"GetAllJobRunning error: {ex}");
            }
        }

        /// <summary>
        /// Lấy toàn bộ job đang chạy thực tế trên site
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public ScheduleInSite GetAllJobRunningOnRing()
        {
            try
            {
                string cronExpression = "";
                string nextFireTime = "";
                string previousFireTime = "";
                TimeSpan offset = TimeSpan.FromHours(7);
                var thisSite = _configuration.Config.Value.ThisSite ?? "";
                ScheduleInSite scheduleInSite = new ScheduleInSite();
                List<ScheduleInfo> scheduleInfos = new List<ScheduleInfo>();

                var jobGroups = this._scheduler.GetJobGroupNames().Result;

                foreach (string group in jobGroups)
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                    var jobKeys = this._scheduler.GetJobKeys(groupMatcher).Result;

                    foreach (var jobKey in jobKeys)
                    {
                        var triggers = this._scheduler.GetTriggersOfJob(jobKey).Result;

                        foreach (ITrigger trigger in triggers)
                        {
                            if (trigger is ICronTrigger cronTrigger)
                            {
                                cronExpression = cronTrigger.CronExpressionString ?? "";
                            }

                            DateTimeOffset? nextFireTimes = trigger.GetNextFireTimeUtc();
                            if (nextFireTimes.HasValue)
                            {
                                nextFireTime = nextFireTimes.Value.ToOffset(offset).ToString("dd/MM/yyyy HH:mm:ss");
                            }

                            DateTimeOffset? previousFireTimes = trigger.GetPreviousFireTimeUtc();
                            if (previousFireTimes.HasValue)
                            {
                                previousFireTime = previousFireTimes.Value.ToOffset(offset).ToString("dd/MM/yyyy HH:mm:ss");
                            }

                            scheduleInfos.Add(new ScheduleInfo
                            {
                                JobKey = new JobKeyInfo
                                {
                                    Group = group,
                                    Name = jobKey.Name
                                },
                                Trigger = new TriggerInfo
                                {
                                    Group = trigger.Key.Group,
                                    Name = trigger.Key.Name,
                                    CronExpression = cronExpression,
                                    NextFireTime = nextFireTime,
                                    PreviousFireTime = previousFireTime,
                                    Status = this._scheduler.GetTriggerState(trigger.Key).Status.ToString(),
                                    Result = this._scheduler.GetTriggerState(trigger.Key).Result.ToString()
                                }
                            });
                        }
                    }
                }

                scheduleInSite = new ScheduleInSite()
                {
                    JobSchedules = scheduleInfos,
                    Site = thisSite
                };

                return scheduleInSite;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllJobRunningOnRing error: {ex}");
                throw new IboxException($"GetAllJobRunningOnRing error: {ex}");
            }
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
