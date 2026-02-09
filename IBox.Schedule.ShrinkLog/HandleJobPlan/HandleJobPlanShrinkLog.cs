using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Schedule.Library.Commom;
using IBox.Schedule.ShrinkLog.Execution;
using IBox.Schedule.ShrinkLog.HandleScheduleDaily;
using IBox.Schedule.ShrinkLog.HubShrinkLogDB;
using IBox.Schedule.ShrinkLog.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace IBox.Schedule.ShrinkLog.HandleJobPlan
{
    public class HandleJobPlanShrinkLog : IHandleJobPlanShrinkLog
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IScheduler _scheduler;
        private readonly IHandleSchedulesDaily _handleSchedulesDaily;
        private readonly IHubShrinkLog _hubShrinkLog;
        private readonly ICommonFunction _commonFunction;

        public HandleJobPlanShrinkLog(IConfiguration configuration, IEncryption encryption, IScheduler scheduler, IHandleSchedulesDaily handleSchedulesDaily, IHubShrinkLog hubShrinkLog, ICommonFunction commonFunction)
        {
            this._configuration = configuration;
            this._encryption = encryption;
            this._scheduler = scheduler;
            this._handleSchedulesDaily = handleSchedulesDaily;
            this._hubShrinkLog = hubShrinkLog;
            _commonFunction = commonFunction;
        }

        /// <summary>
        /// Thực hiện tạo job trên schedule từ plan
        /// </summary>
        public void ExecuteJobPlanShrinkLog()
        {
            var thisSite = this._configuration.Config.Value.ThisSite ?? "";

            var rootContext = new RootContext(this._configuration, this._encryption).Context;

            var listPlans = rootContext.S_PlanShrinkLogs.Where(ptr =>
                            !ptr.IsDelete
                            && ptr.SiteRunning == thisSite
                            && ptr.StatusPlan == StatusPlanShrinkLog.Await
                            && ptr.CreatedDate != null
                            && DateTime.Now.Date == ptr.CreatedDate.Value.Date).ToList();

            foreach (var plan in listPlans)
            {
                try
                {
                    CreateScheduleJob(plan, thisSite);

                    rootContext.S_PlanShrinkLogs.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(rootContext, new S_PlanShrinkLog()
                    {
                        Id = plan.Id,
                        StatusPlan = StatusPlanShrinkLog.Await
                    });

                }
                catch (Exception ex)
                {
                    rootContext.S_PlanShrinkLogs.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(rootContext, new S_PlanShrinkLog()
                    {
                        Id = plan.Id,
                        StatusPlan = StatusPlanShrinkLog.Fail
                    });

                    Log.Error($"Execute Job Plan Shrink Log an error has occurred: {ex}");
                }
            }
        }

        /// <summary>
        /// Lấy tất cả các job đang chạy thực tế trên schedule của tất cả các server root
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public List<ScheduleInSite> GetAllJobRunning(HttpRequest requestContext)
        {
            try
            {
                List<ScheduleInSite> result = new List<ScheduleInSite>();
                var authorization = requestContext.Headers["Authorization"].ToString();
                IBGlobalConfig.RootServersIBox.ToList().ForEach(ip =>
                {
                    try
                    {
                        var rest = string.Format(@"{0}{1}", ip, "/api/Job/GetAllJobRunningOnRing");
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                        requestMessage.Headers.Add("Authorization", authorization);
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
                                Log.Error("GetAllJobRunning an error has occurred because data GetAllJobRunningOnRing is null.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GetAllJobRunning an error has occurred because: {ex}");
                    }
                });

                return result;
            }
            catch (Exception ex)
            {
                throw new IboxException($"GetAllJobRunning error because: {ex}");
            }
        }

        /// <summary>
        /// Lấy danh sách job đang chạy thực tế trên schedule trên server
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public ResScheduleInSite GetAllJobRunningOnRing()
        {
            try
            {
                return _commonFunction.GetAllJobRunningOnRing();
                //string cronExpression = "";
                //string nextFireTime = "";
                //string previousFireTime = "";
                //TimeSpan offset = TimeSpan.FromHours(7);
                //var thisSite = _configuration.Config.Value.ThisSite ?? "";
                //ScheduleInSite scheduleInSite = new ScheduleInSite();
                //List<ScheduleInfo> scheduleInfos = new List<ScheduleInfo>();

                //var jobGroups = this._scheduler.GetJobGroupNames().Result;

                //foreach (string group in jobGroups)
                //{
                //    var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                //    var jobKeys = this._scheduler.GetJobKeys(groupMatcher).Result;

                //    foreach (var jobKey in jobKeys)
                //    {
                //        var triggers = this._scheduler.GetTriggersOfJob(jobKey).Result;

                //        foreach (ITrigger trigger in triggers)
                //        {
                //            if (trigger is ICronTrigger cronTrigger)
                //            {
                //                cronExpression = cronTrigger.CronExpressionString ?? "";
                //            }

                //            DateTimeOffset? nextFireTimes = trigger.GetNextFireTimeUtc();
                //            if (nextFireTimes.HasValue)
                //            {
                //                nextFireTime = nextFireTimes.Value.ToOffset(offset).ToString("dd/MM/yyyy HH:mm:ss");
                //            }

                //            DateTimeOffset? previousFireTimes = trigger.GetPreviousFireTimeUtc();
                //            if (previousFireTimes.HasValue)
                //            {
                //                previousFireTime = previousFireTimes.Value.ToOffset(offset).ToString("dd/MM/yyyy HH:mm:ss");
                //            }

                //            scheduleInfos.Add(new ScheduleInfo
                //            {
                //                JobKey = new JobKeyInfo
                //                {
                //                    Group = group,
                //                    Name = jobKey.Name
                //                },
                //                Trigger = new TriggerInfo
                //                {
                //                    Group = trigger.Key.Group,
                //                    Name = trigger.Key.Name,
                //                    CronExpression = cronExpression,
                //                    NextFireTime = nextFireTime,
                //                    PreviousFireTime = previousFireTime,
                //                    Status = this._scheduler.GetTriggerState(trigger.Key).Status.ToString(),
                //                    Result = this._scheduler.GetTriggerState(trigger.Key).Result.ToString()
                //                }
                //            });
                //        }
                //    }
                //}

                //scheduleInSite = new ScheduleInSite()
                //{
                //    JobSchedules = scheduleInfos,
                //    Site = thisSite
                //};

                return scheduleInSite;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllJobRunningOnRing error: {ex}");
                throw new IboxException($"GetAllJobRunningOnRing error: {ex}");
            }
        }

        /// <summary>
        /// Dừng job schedule
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
        public bool StopJobSchedule(HttpRequest requestContext, RequestJobSchedule requestJobSchedule)
        {
            try
            {
                var authorize = requestContext.Headers["Authorization"].ToString();

                var bodyReq = JsonConvert.SerializeObject(requestJobSchedule);

                CallApiStopJobScheduleOnRing(authorize, bodyReq);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Stop job schedule an error has occurred: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Khởi chạy job schedule
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        public bool StartJobSchedule(HttpRequest requestContext, RequestJobSchedule req)
        {
            try
            {
                var bodyReq = JsonConvert.SerializeObject(req);
                var authorize = requestContext.Headers["Authorization"].ToString();
                var dataRequest = new StringContent(bodyReq, Encoding.UTF8, "application/json");

                IBGlobalConfig.RootServersIBox.ToList().ForEach(ip =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var rest = string.Format(@"{0}{1}", ip, "/api/Job/StartJobScheduleOnRing");
                            var handler = new HttpClientHandler();
                            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                            HttpClient httpClient = new HttpClient(handler);
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                            requestMessage.Headers.Add("Authorization", authorize);
                            requestMessage.Content = dataRequest;
                            httpClient.SendAsync(requestMessage).Wait();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Can't start job to server: {ip} because; {ex}");
                        }
                    });
                });

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Start job schedule an error has occurred: {ex}");

                return false;
            }
        }

        /// <summary>
        /// Chạy job ngay lập tức
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public bool StartJobNow(HttpRequest requestContext, S_ScheduleShrinkLog req)
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Start_Job_Now start.");

                var authorization = requestContext.Headers["Authorization"].ToString();

                if (string.IsNullOrEmpty(req?.Site?.Trim()))
                {
                    throw new IboxException($"Site can't null or empty");
                }

                var rootContext = new RootContext(this._configuration, this._encryption);

                var schedule = rootContext.Context.S_ScheduleShrinkLogs.FirstOrDefault(ptr =>
                                !ptr.IsDelete
                                && ptr.Site == req.Site
                                && ptr.Id == req.Id);

                if (schedule == null)
                {
                    Log.Error("Start job now fail because schedule is null.");

                    throw new IboxException($"Start job now fail because schedule is null.");
                }

                var plan = rootContext.Context.S_PlanShrinkLogs.FirstOrDefault(ptr =>
                                    ptr.ScheduleId == schedule.Id
                                    && !ptr.IsDelete
                                    && ptr.CreatedDate != null
                                    && DateTime.Now.Date == ptr.CreatedDate.Value.Date);

                if (plan != null)
                {
                    plan.TryUpdate(rootContext.Context, new S_PlanShrinkLog
                    {
                        Id = plan.Id,
                        IsDelete = true
                    });

                    var requestStopSchedule = new RequestJobSchedule()
                    {
                        JobKey = plan.KeyTrigg
                    };

                    var bodyStopSchedule = JsonConvert.SerializeObject(requestStopSchedule);

                    CallApiStopJobScheduleOnRing(authorization, bodyStopSchedule);
                }

                this._handleSchedulesDaily.MakePlanForRoot(req.Id, req.Site);

                var listServerSite = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == TypeService.RootBE).ToList();

                var serviceRun = listServerSite.Find(x => x.Site == req.Site);

                if (serviceRun == null)
                {
                    Log.Error($"Site {req.Site} is not found in list server site.");

                    throw new IboxException($"Site {req.Site} is not found in list server site.");
                }

                var data = JsonConvert.SerializeObject(req);

                // Run job for ibox deploy standAlone
                if (listServerSite.Count == 1 && listServerSite[0].Site == thisSite)
                {
                    CallApiRunSpecifiedJob(listServerSite[0], authorization, data);

                    return true;
                }

                if (serviceRun.Site == thisSite)
                {
                    CallApiRunSpecifiedJob(serviceRun, authorization, data);

                    return true;
                }

                var urlHub = $"{BuildUrlHub(serviceRun)}/ChatHubShrinkLog";

                var isReachable = this._hubShrinkLog.CheckHubReconnected(urlHub);

                if (isReachable)
                {
                    CallApiRunSpecifiedJob(serviceRun, data, authorization);

                    return true;
                }
                else
                {
                    Log.Information($"Hub {urlHub} is not reachable.");

                    foreach (var serverSite in listServerSite)
                    {
                        string urlHubRun = $"{BuildUrlHub(serverSite ?? new S_Service())}/ChatHubShrinkLog";

                        bool isCheckSiteRun = this._hubShrinkLog.CheckHubReconnected(urlHubRun);

                        if (isCheckSiteRun)
                        {
                            CallApiRunSpecifiedJob(serverSite ?? new S_Service(), data, authorization);

                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Start job now an error has occurred: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Call api stop job tới tất cả server root
        /// </summary>
        /// <param name="authorize"></param>
        /// <param name="bodyReq"></param>
        private void CallApiStopJobScheduleOnRing(string authorize, string bodyReq)
        {
            StringContent bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.RootServersIBox.ToList().ForEach(ip =>
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
                        requestMessage.Headers.Add("Authorization", authorize);
                        requestMessage.Content = bodyReqStopSchedule;
                        httpClient.SendAsync(requestMessage).Wait();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Can't stop job to server: {ip} because {ex.Message}");
                    }
                });
            });

            Thread.Sleep(1500);
        }

        /// <summary>
        /// Call api chạy job ngay lập tức tới server root chỉ định
        /// </summary>
        /// <param name="serviceRun"></param>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        private void CallApiRunSpecifiedJob(S_Service serviceRun, string bodyReq, string authorization)
        {
            var siteRun = IBGlobalConfig.RootServersIBox.Find(x => x.Contains(serviceRun.Site ?? ""));

            StringContent body = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            Task.Run(() =>
            {
                try
                {
                    var rest = string.Format(@"{0}{1}", siteRun, "/api/Job/RunSpecifiedJob");
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    HttpClient httpClient = new HttpClient(handler);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                    requestMessage.Headers.Add("Authorization", authorization);
                    requestMessage.Content = body;
                    httpClient.SendAsync(requestMessage).Wait();
                }
                catch (Exception ex)
                {
                    Log.Error($"Call api RunSpecifiedJob to {siteRun} an error has occurred: {ex}");
                }
            });
        }

        /// <summary>
        /// Dừng công việc đang chạy trên site
        /// </summary>
        /// <param name="requestJobSchedule"></param>
        /// <returns></returns>
		public bool StopJobScheduleOnRing(RequestJobSchedule requestJobSchedule)
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Stop_Job: Begin stop job {requestJobSchedule.JobKey} in site {thisSite}");

                if (string.IsNullOrEmpty(requestJobSchedule.JobKey))
                {
                    Log.Error($"Stop job fail because JobKey is null or empty.");
                    return false;
                }

                if (!this._scheduler.IsStarted)
                {
                    this._scheduler.Start();
                    Thread.Sleep(1500);
                }

                var jobKeyStop = new JobKey($"{requestJobSchedule.JobKey}", "Group2");

                if (!this._scheduler.IsStarted)
                {
                    Log.Error($"Stop job fail because scheduler is not start in site {thisSite}.");
                    return false;
                }

                bool jobIsExists = this._scheduler.CheckExists(jobKeyStop).Result;

                if (jobIsExists)
                {
                    this._scheduler.DeleteJob(jobKeyStop);

                    Log.Information($"Stop job {requestJobSchedule.JobKey} in site {thisSite} success.");
                    return true;
                }
                else
                {
                    Log.Error($"Stop job fail because job {requestJobSchedule.JobKey} is not exists in site {thisSite}.");
                    return false;
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Stop job an error has occurred: {ex}");
                return false;
            }
        }

        /// <summary>
		/// Khởi động lại công việc đã tắt trên site
		/// </summary>
		/// <param name="tenantId"></param>
		/// <param name="requestJobSchedule"></param>
		/// <returns></returns>
		public bool StartJobScheduleOnRing(RequestJobSchedule req)
        {
            var rootContext = new RootContext(this._configuration, this._encryption).Context;

            try
            {
                string thisSite = this._configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"Start_Job: Begin start job: {req.JobKey} in site: {thisSite}.");

                if (string.IsNullOrEmpty(req.JobKey))
                {
                    Log.Error("$Start_Job fail because JobKey is null or empty.");
                    return false;
                }

                if (string.IsNullOrEmpty(thisSite))
                {
                    Log.Error("$Start_Job fail because not found this site to run.");
                    return false;
                }

                var plan = rootContext
                    .S_PlanShrinkLogs
                    .Where(ptr => ptr.KeyTrigg == req.JobKey && !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date)
                    .FirstOrDefault();

                if (plan == null)
                {
                    Log.Error($"Start_Job fail because not found keyJob {req.JobKey}.");
                    rootContext.Dispose();
                    return false;
                }

                if (string.IsNullOrEmpty(plan.FormatCron))
                {
                    Log.Error("Start_Job fail because formatCron of job is null or empty.");
                    rootContext.Dispose();
                    return false;
                }

                if (plan.SiteRunning != thisSite)
                {
                    Log.Error($"Start_Job fail because Job {req.JobKey} not match to run in site: {thisSite}.");
                    rootContext.Dispose();
                    return false;
                }

                CreateScheduleJob(plan, thisSite);

                plan.TryUpdate(rootContext, new S_PlanShrinkLog()
                {
                    Id = plan.Id,
                    StatusPlan = StatusPlanShrinkLog.Complete
                });

                rootContext.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Start_Job error because: {ex}");
                rootContext.Dispose();
                return false;
            }
        }

        /// <summary>
		/// Chạy ngay job được chỉ định
		/// </summary>
		/// <param name="ternantId"></param>
		/// <param name="req"></param>
		/// <returns></returns>
		/// <exception cref="IboxException"></exception>
		public bool RunSpecifiedJob(S_ScheduleShrinkLog req)
        {
            var rootContext = new RootContext(this._configuration, this._encryption);

            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? "";

                Log.Information($"RunSpecifiedJob: start job now in site {thisSite}.");

                var plan = rootContext.Context.S_PlanShrinkLogs.FirstOrDefault(ptr =>
                                                                                !ptr.IsDelete
                                                                                && ptr.KeyTrigg == $"Job_{req.Id}_{req.Site}"
                                                                                && ptr.CreatedDate != null
                                                                                && DateTime.Now.Date == ptr.CreatedDate.Value.Date);

                if (plan == null)
                {
                    Log.Error($"RunSpecifiedJob error: Because not found plan with jobkey Job_{req.Id}_{req.Site} in site {thisSite}.");
                    throw new IboxException("Not found plan.");
                }

                try
                {
                    CreateScheduleJob(plan, thisSite);

                    rootContext.Context.S_PlanShrinkLogs.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(rootContext.Context, new S_PlanShrinkLog()
                    {
                        Id = plan.Id,
                        StatusPlan = StatusPlanShrinkLog.Complete,
                        SiteRunning = thisSite
                    });
                }
                catch (Exception ex)
                {
                    Log.Error($"RunSpecifiedJob an error has occurred: {ex}");

                    rootContext.Context.S_PlanShrinkLogs.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(rootContext.Context, new S_PlanShrinkLog()
                    {
                        Id = plan.Id,
                        StatusPlan = StatusPlanShrinkLog.Fail,
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error($"RunSpecifiedJob error because: {ex}");

                rootContext.Dispose();

                throw new IboxException($"RunSpecifiedJob error because: {ex}");
            }

            rootContext.Dispose();

            return true;
        }

        /// <summary>
        /// Trả về url hub
        /// </summary>
        /// <param name="s_Service"></param>
        /// <returns></returns>
        private string BuildUrlHub(S_Service s_Service)
        {
            if (s_Service == null)
            {
                return string.Empty;
            }

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

        /// <summary>
        /// Tạo job trên schedule
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="thisSite"></param>
        /// <exception cref="IboxException"></exception>
        private void CreateScheduleJob(S_PlanShrinkLog plan, string thisSite)
        {
            if (!this._scheduler.IsStarted)
            {
                this._scheduler.Start();
                Thread.Sleep(1000);
            }

            if (this._scheduler.IsStarted)
            {
                var jobKey = new JobKey($"Job_{plan.ScheduleId}_{plan.Site}", "Group2");
                var jobTrigger = new TriggerKey($"Trigg_{plan.ScheduleId}_{plan.Site}", "Group2");

                bool isJobExists = this._scheduler.CheckExists(jobKey).Result;

                if (isJobExists)
                {
                    Log.Information($"Delete Job_{plan.ScheduleId}_{plan.Site} in site {thisSite}.");

                    this._scheduler.UnscheduleJob(jobTrigger);

                    this._scheduler.DeleteJob(jobKey);
                }

                if (string.IsNullOrEmpty(plan.FormatCron))
                {
                    throw new IboxException($"CreateScheduleJob fail because formatCron of Job_{plan.Id}_{plan.Site} is null or empty.");
                }

                var job = JobBuilder.Create<ExcuteShrinkLog>()
                    .UsingJobData("planId", plan.Id)
                    .UsingJobData("listCategory", plan.ListCategory)
                    .UsingJobData("scheduleId", plan.ScheduleId)
                    .UsingJobData("deploymentType", ((int?)plan.DeploymentType).ToString())
                    .UsingJobData("jobKey", jobKey.Name)
                    .UsingJobData("methodShrinkLog", plan.MethodShrinkLog.ToString())
                    .WithIdentity(jobKey)
                    .Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(jobTrigger)
                    .WithCronSchedule(plan.FormatCron)
                    .Build();

                this._scheduler.ScheduleJob(job, trigger);

                Log.Information($"CreateScheduleJob job Job_{plan.ScheduleId}_{plan.Site} in site: {thisSite} success.");

            }
            else
            {
                throw new IboxException($"CreateScheduleJob error because scheduler is not start in site {thisSite}.");
            }
        }
    }
}
