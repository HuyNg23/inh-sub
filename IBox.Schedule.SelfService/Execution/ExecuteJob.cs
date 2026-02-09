using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Newtonsoft.Json;
using Quartz;
using Serilog;

namespace IBox.Schedule.SelfService.Execution
{
    public class ExecuteJob : IExecuteJob
    {
        private readonly IScheduler _scheduler;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public ExecuteJob(IScheduler scheduler, IConfiguration configuration, IEncryption encryption)
        {
            this._scheduler = scheduler;
            this._configuration = configuration;
            this._encryption = encryption;
        }

        public void RunJobDay()
        {
            var thisSite = this._configuration.Config.Value.ThisSite ?? "";

            var rootContext = new RootContext(this._configuration, this._encryption);

            var allTenants = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

            rootContext.Context.Dispose();

            foreach (var itemTenants in allTenants)
            {
                var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

                var tenantContexts = context.GetTenantContext(itemTenants.Id).Context;

                var listPlans = tenantContexts.S_ImplementationPlans.Where(ptr => !ptr.IsDelete 
                                                                                && ptr.SiteRunning == thisSite
                                                                                && ptr.StatusPlan == StatusPlan.Await
                                                                                && ptr.CreatedDate != null
                                                                                && DateTime.Now.Date == ptr.CreatedDate.Value.Date).ToList();

                foreach (var plan in listPlans)
                {
                    try
                    {
                        if (_scheduler.IsStarted)
                        {
                            var jobKey = new JobKey($"Job_{plan.ScheduleID}_{plan.Site}", "Group2");
                            var jobTrigger = new TriggerKey($"Trigg_{plan.ScheduleID}_{plan.Site}", "Group2");

                            bool isJobExists = _scheduler.CheckExists(jobKey).Result;

                            Log.Information($"RunJobDay CheckExists jobKey: {jobKey} is {isJobExists}");

                            if (isJobExists)
                            {
                                this._scheduler.UnscheduleJob(jobTrigger);
                                _scheduler.DeleteJob(jobKey);
                            }

                            //Convert thời gian chạy thành Cron
                            string ConvertCron = !string.IsNullOrEmpty(plan.FormatCron) ? plan.FormatCron : $"{plan.Second} {plan.Minute} {plan.Hour} ? {plan.Month} {plan.Day} {plan.Year}";

                            var job = JobBuilder.Create<ExcuteWorkflow>()
                                .UsingJobData("wfid", plan.Wfid)
                                .UsingJobData("planId", plan.Id)
                                .UsingJobData("scheduleId", plan.ScheduleID)
                                .UsingJobData("tenantId", itemTenants.Id)
                                .UsingJobData("typeSchedule", ((int?)plan.Type).ToString())
                                .UsingJobData("jobKey", jobKey.Name)
                                .UsingJobData("isDeploy", "False")
                                .WithIdentity(jobKey)
                                .Build();

                            ITrigger trigger = TriggerBuilder.Create()
                                .WithIdentity(jobTrigger)
                                .WithCronSchedule(ConvertCron)
                                .Build();

                            _scheduler.ScheduleJob(job, trigger);

                            tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                            {
                                Id = plan.Id,
                                StatusPlan = StatusPlan.Complete
                            });
                        }
                        else
                        {
                            tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                            {
                                Id = plan.Id,
                                StatusPlan = StatusPlan.Await
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == plan.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                        {
                            Id = plan.Id,
                            StatusPlan = StatusPlan.Fail,
                        });
                        Log.Error("itemPlanScheduler: " + JsonConvert.SerializeObject(plan));
                        Log.Error($"RunJobDay_Exception: {ex}");
                    }
                }

                tenantContexts.Context.Dispose();
            }
        }

        public void RunJobServer(string serverNameError, string serverNameRun)
        {
            Log.Information($"Run switch job server Started. SiteError: {serverNameError}, SiteRun: {serverNameRun}");

            var thisSite = this._configuration.Config.Value.ThisSite ?? "";

            var rootContext = new RootContext(this._configuration, this._encryption);

            var allTenants = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

            rootContext.Context.Dispose();

            foreach (var itemTenants in allTenants)
            {
                var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

                var tenantContexts = context.GetTenantContext(itemTenants.Id).Context;

                if (serverNameRun == thisSite)
                {
                    var listPlanScheduler = tenantContexts.S_ImplementationPlans
                        .Where(ptr => !ptr.IsDelete && ptr.CreatedDate != null && ptr.CreatedDate.Value.Date == DateTime.Now.Date && ptr.SiteRunning == serverNameError)
                        .ToList();

                    foreach (var planScheduler in listPlanScheduler)
                    {
                        try
                        {
                            if (_scheduler.IsStarted)
                            {
                                var jobKey = new JobKey($"Job_{planScheduler.ScheduleID}_{planScheduler.Site}", "Group2");
                                var jobTrigger = new TriggerKey($"Trigg_{planScheduler.ScheduleID}_{planScheduler.Site}", "Group2");

                                bool isJobExists = _scheduler.CheckExists(jobKey).Result;

                                Log.Information($"RunJobServer CheckExists jobKey: {jobKey} is {isJobExists}");

                                if (isJobExists)
                                {
                                    _scheduler.DeleteJob(jobKey);
                                }

                                string ConvertCron = !string.IsNullOrEmpty(planScheduler.FormatCron) ? planScheduler.FormatCron : $"{planScheduler.Second} {planScheduler.Minute} {planScheduler.Hour} ? {planScheduler.Month} {planScheduler.Day} {planScheduler.Year}";

                                var job = JobBuilder.Create<ExcuteWorkflow>()
                                    .UsingJobData("wfid", planScheduler.Wfid)
                                    .UsingJobData("planId", planScheduler.Id)
                                    .UsingJobData("scheduleId", planScheduler.ScheduleID)
                                    .UsingJobData("tenantId", itemTenants.Id)
                                    .UsingJobData("typeSchedule", ((int?)planScheduler.Type).ToString())
                                    .UsingJobData("jobKey", jobKey.Name)
                                    .UsingJobData("isDeploy", "False")
                                    .WithIdentity(jobKey)
                                    .Build();

                                ITrigger trigger = TriggerBuilder.Create()
                                    .WithIdentity(jobTrigger)
                                    .WithCronSchedule(ConvertCron)
                                    .Build();

                                _scheduler.ScheduleJob(job, trigger);

                                tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == planScheduler.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                                {
                                    Id = planScheduler.Id,
                                    StatusPlan = StatusPlan.Complete,
                                    SiteRunning = serverNameRun
                                });
                            }
                            else
                            {
                                tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == planScheduler.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                                {
                                    Id = planScheduler.Id,
                                    StatusPlan = StatusPlan.Await
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            tenantContexts.S_ImplementationPlans.FirstOrDefault(ptr => ptr.Id == planScheduler.Id).TryUpdate(tenantContexts, new S_ImplementationPlan()
                            {
                                Id = planScheduler.Id,
                                StatusPlan = StatusPlan.Fail,
                            });
                            Log.Error("itemPlanScheduler: " + JsonConvert.SerializeObject(planScheduler));
                            Log.Error($"RunJobServer_Exception: {ex}");
                        }
                    }
                }

                tenantContexts.Context.Dispose();
            }
        }
    }
}
