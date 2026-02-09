using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant.Tables;
using IBox.Schedule.Library.Execution;
using IBox.Schedule.Library.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl.Matchers;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using TypeScheduleBase = IBox.Schedule.Library.Model.TypeScheduleBase;

namespace IBox.Schedule.Library.Commom
{
    public class CommonFunction : ICommonFunction
    {
        private readonly IScheduler _scheduler;

        public CommonFunction(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        private void CallApi(string url, string authorization, StringContent content)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ClientCertificateOptions = ClientCertificateOption.Manual
                };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                HttpClient httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Headers = { { "Authorization", authorization } },
                    Content = content
                };

                httpClient.Send(requestMessage);
            }
            catch (Exception ex)
            {
                Log.Error($"API call to {url} failed: {ex}");
            }
        }

        /// <summary>
        /// Gọi Api dừng job được chỉ định mà nó đang chạy trên các site schedule của tenant
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        public void CallApiStopJobOnRingTenant(string bodyReq, string authorization)
        {
            var bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.ScheduleServiceIBox.ToList().ForEach(ip =>
            {
                var url = $"{ip}/api/Job/StopJobScheduleOnRing";
                CallApi(url, authorization, bodyReqStopSchedule);
            });
        }

        /// <summary>
        /// Gọi Api dừng job được chỉ định mà nó đang chạy trên các site schedule của root
        /// </summary>
        /// <param name="bodyReq"></param>
        /// <param name="authorization"></param>
        public void CallApiStopJobOnRingRoot(string bodyReq, string authorization)
        {
            var bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.RootServersIBox.ToList().ForEach(ip =>
            {
                var url = $"{ip}/api/Job/StopJobScheduleOnRing";
                CallApi(url, authorization, bodyReqStopSchedule);
            });
        }

        public void CallApiStartJobOnRingTenant(string bodyReq, string authorization)
        {
            StringContent bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.ScheduleServiceIBox.ToList().ForEach(ip =>
            {
                Task.Run(() =>
                {
                    var url = $"{ip}/api/Job/StartJobScheduleOnRing";
                    CallApi(url, authorization, bodyReqStopSchedule);
                });
            });
        }

        public void CallApiStartJobOnRingRoot(string bodyReq, string authorization)
        {
            StringContent bodyReqStopSchedule = new StringContent(bodyReq, Encoding.UTF8, "application/json");

            IBGlobalConfig.RootServersIBox.ToList().ForEach(ip =>
            {
                var url = $"{ip}/api/Job/StartJobScheduleOnRing";
                CallApi(url, authorization, bodyReqStopSchedule);
            });
        }

        public void CallApiRunSpecifiedJobTenant(S_Service service, string authorization, string bodyReq)
        {
            var site = IBGlobalConfig.ScheduleServiceIBox.Find(x => x.Contains(service.Site ?? ""));

            if (site == null)
            {
                Log.Error($"Site {service.Site} is not found in list ScheduleServiceIBox.");
                return;
            }

            var dataRequest = new StringContent(bodyReq, Encoding.UTF8, "application/json");
            Task.Run(() =>
            {
                var url = $"{site}/api/Job/RunSpecifiedJob";
                CallApi(url, authorization, dataRequest);
            });
        }

        public void CallApiRunSpecifiedJobRoot(S_Service service, string authorization, string bodyReq)
        {
            var site = IBGlobalConfig.RootServersIBox.Find(x => x.Contains(service.Site ?? ""));

            if (site == null)
            {
                Log.Error($"Site {service.Site} is not found in list RootServersIBox.");
                return;
            }

            var dataRequest = new StringContent(bodyReq, Encoding.UTF8, "application/json");
            Task.Run(() =>
            {
                var url = $"{site}/api/Job/RunSpecifiedJob";
                CallApi(url, authorization, dataRequest);
            });
        }

        public ScheduleBase ConvertToScheduleBase(S_ScheduleShrinkLog schedule)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            return new ScheduleBase
            {
                Type = (TypeScheduleBase)schedule.Type,
                Hour = schedule.Hour,
                Minute = schedule.Minute,
                Day = schedule.Day,
                Weekday = schedule.Weekday,
                Site = schedule.Site,
                Id = schedule.Id
            };
#pragma warning restore CS8629 // Nullable value type may be null.
        }

        public ScheduleBase ConvertToScheduleBase(S_Schedule schedule)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            return new ScheduleBase
            {
                Type = (TypeScheduleBase)schedule.Type,
                Hour = schedule.Hour,
                Minute = schedule.Minute,
                Day = schedule.Day,
                Weekday = schedule.Weekday,
                Site = schedule.Site,
                Id = schedule.Id
            };
#pragma warning restore CS8629 // Nullable value type may be null.
        }

        public string GetCronExpression(ScheduleBase schedule)
        {
            switch (schedule.Type)
            {
                case TypeScheduleBase.NumberOfHoursMinutes:
                    if (schedule.Hour == "00")
                    {
                        return $"0 0/{FormatTime(schedule.Minute ?? "1")} * * * ?";
                    }
                    else if (schedule.Minute == "00")
                    {
                        return $"0 0 */{FormatTime(schedule.Hour ?? "1")} ? * *";
                    }
                    else
                    {
                        return $"0 {FormatTime(schedule.Minute ?? "1")} */{FormatTime(schedule.Hour ?? "1")} ? * *";
                    }

                case TypeScheduleBase.Daily:
                    return $"0 {schedule.Minute} {schedule.Hour} * * ? *";

                case TypeScheduleBase.Weekly:
                    return $"0 {schedule.Minute} {schedule.Hour} ? * {schedule.Weekday}";

                case TypeScheduleBase.Monthly:
                    return $"0 {schedule.Minute} {schedule.Hour} {schedule.Day} * ?";

                default:
                    Log.Error($"Not found type schedule {schedule.Id}");
                    throw new IboxLog($"Not found type {schedule.Type} schedule {schedule.Id}", "AppLogs");
            }
        }

        private string FormatTime(string time)
        {
            if (time.StartsWith('0'))
            {
                return time.Remove(0, 1);
            }

            return time;
        }

        public bool CheckScheduleStart()
        {
            return _scheduler.IsStarted;
        }

        public void StartSchedule()
        {
            _scheduler.Start();
            Thread.Sleep(2000);
        }

        public bool CheckExistsJobInSchedule(string nameJobKey)
        {
            return _scheduler.CheckExists(GetJobKey(nameJobKey)).Result;
        }

        /// <summary>
        /// Xóa job schedule trên quartz
        /// </summary>
        /// <param name="nameJobKey"></param>
        /// <param name="nameTrigger"></param>
        public void DeleteJobInSchedule(string nameJobKey, string nameTrigger)
        {
            if (!CheckScheduleStart())
            {
                StartSchedule();
            }

            if (CheckScheduleStart())
            {
                var jobKey = GetJobKey(nameJobKey);

                var triggers = _scheduler.GetTriggersOfJob(jobKey).Result;

                foreach (var trigger in triggers)
                {
                    _scheduler.UnscheduleJob(trigger.Key).Wait();
                }

                _scheduler.DeleteJob(jobKey).Wait();

                Log.Information($"Delete job {nameJobKey} and all associated triggers success.");
            }
            else
            {
                Log.Error($"DeleteJobInSchedule error because schedule is not started.");
            }
        }

        /// <summary>
        /// Tạo job schedule trên quartz cho tenant
        /// </summary>
        /// <param name="req"></param>
        public void CreateScheduleJobTenant(ReqCreateScheduleJob req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.CronExpression))
                {
                    Log.Information($"Create job {req.ScheduleId} fail because CronExpression is null or empty.");
                    return;
                }

                var jobKey = new JobKey($"Job_{req.JobName}", "Group2");
                var jobTrigger = new TriggerKey($"Trigg_{req.ScheduleId}_{req.Site}_{req.TenantId}", "Group2");

                var job = JobBuilder.Create<ExecuteWorkflow>()
                   .UsingJobData("wfid", req.WfId)
                   .UsingJobData("scheduleId", req.ScheduleId)
                   .UsingJobData("tenantId", req.TenantId)
                   .UsingJobData("typeSchedule", ((int?)req.TypeSchedule).ToString())
                   .UsingJobData("jobKey", jobKey.Name)
                   .WithIdentity(jobKey)
                   .Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity(jobTrigger)
                    .WithCronSchedule(req.CronExpression)
                    .Build();

                _scheduler.ScheduleJob(job, trigger);

                Log.Information($"Create job {jobKey.Name} success.");
            }
            catch (Exception ex)
            {
                Log.Error($"CreateScheduleJobTenant: {ex.Message}\n req: {JsonConvert.SerializeObject(req)}");
            }
        }

        /// <summary>
        /// Tạo job schedule trên quartz cho root
        /// </summary>
        /// <param name="req"></param>
        public void CreateScheduleJobRoot(ReqCreateScheduleJob req)
        {
            if (string.IsNullOrEmpty(req.CronExpression))
            {
                Log.Information($"Create job {req.ScheduleId} fail because CronExpression is null or empty.");
                return;
            }

            var jobKey = new JobKey($"Job_{req.JobName}", "Group2");
            var jobTrigger = new TriggerKey($"Trigg_{req.ScheduleId}_{req.Site}", "Group2");

            var job = JobBuilder.Create<ExecuteShrinkLog>()
                .UsingJobData("listCategory", req.ListCategory)
                .UsingJobData("scheduleId", req.ScheduleId)
                .UsingJobData("typeSchedule", ((int?)req.TypeSchedule).ToString())
                .UsingJobData("deploymentType", ((int?)req.DeploymentType).ToString())
                .UsingJobData("jobKey", jobKey.Name)
                .UsingJobData("methodShrinkLog", req.MethodShrinkLog.ToString())
               .WithIdentity(jobKey)
               .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(jobTrigger)
                .WithCronSchedule(req.CronExpression)
                .Build();

            _scheduler.ScheduleJob(job, trigger);

            Log.Information($"Create job {jobKey.Name} success.");
        }

        public List<ResScheduleInSite> GetAllJobRunning(HttpRequest requestContext, List<string> servers)
        {
            try
            {
                List<ResScheduleInSite> result = new List<ResScheduleInSite>();
                var authorization = requestContext.Headers["Authorization"].ToString();

                servers.ForEach(ip =>
                {
                    try
                    {
                        var rest = $"{ip}/api/Job/GetAllJobRunningOnRing";
                        using (var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual })
                        using (var httpClient = new HttpClient(handler))
                        {
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            var requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                            requestMessage.Headers.Add("Authorization", authorization);

                            var httpResponse = httpClient.Send(requestMessage);

                            if (httpResponse.StatusCode == HttpStatusCode.OK)
                            {
                                var responseContent = httpResponse.Content.ReadAsStringAsync().Result;
                                var data = JsonConvert.DeserializeObject<ResponseForm<ResScheduleInSite>>(responseContent);

                                if (data != null && data.Data != null)
                                {
                                    result.Add(data.Data);
                                }
                                else
                                {
                                    new IboxLog($"Data GetAllJobRunningOnRing is null.", "AppLogs");
                                }
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
                throw new IboxLog($"GetAllJobRunning error: {ex}", "AppLogs");
            }
        }

        public ResScheduleInSite GetAllJobRunningOnRing(string? tenantId = "")
        {
            try
            {
                string cronExpression = "";
                string nextFireTime = "";
                string previousFireTime = "";
                TimeSpan offset = TimeSpan.FromHours(7);
                var thisSite = IBGlobalConfig.ThisSite ?? "";
                ResScheduleInSite scheduleInSite = new ResScheduleInSite();
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
                            if (!string.IsNullOrEmpty(tenantId) && !trigger.Key.Name.Contains(tenantId))
                            {
                                continue;
                            }

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
                                JobKey = new JobKeyScheduleInfo
                                {
                                    Group = group,
                                    Name = jobKey.Name
                                },
                                Trigger = new TriggerScheduleInfo
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

                scheduleInSite = new ResScheduleInSite()
                {
                    JobSchedules = scheduleInfos,
                    Site = thisSite
                };

                return scheduleInSite;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"GetAllJobRunningOnRing error: {ex}", tenantId ?? "AppLogs");
            }
        }

        public string BuildUrlHub(S_Service s_Service)
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

        public string FormatNameJobKey(string jobName)
        {
            return $"Job_{jobName}";
        }

        public string FormatNameTrigger(string scheduleId, string site, string tenantId)
        {
            return $"Trigg_{scheduleId}_{site}_{tenantId}";
        }

        private JobKey GetJobKey(string name)
        {
            return new JobKey(name, "Group2");
        }

        public void DeleteJobSchedule(S_Schedule schedule)
        {
            try
            {
                if (schedule == null || string.IsNullOrEmpty(schedule.Id))
                {
                    return;
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
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteJobSchedule: {ex.Message}");
            }
        }
    }
}