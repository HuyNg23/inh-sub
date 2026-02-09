using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Schedule.ShrinkLog.Model;
using Serilog;

namespace IBox.Schedule.ShrinkLog.HandleScheduleDaily
{
    public class HandleSchedulesDaily : IHandleSchedulesDaily
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public HandleSchedulesDaily(IConfiguration configuration, IEncryption encryption)
        {
            _configuration = configuration;
            _encryption = encryption;
        }

        /// <summary>
        /// Thêm schedule vào plan
        /// </summary>
        /// <returns></returns>
        public bool InsertScheduleToPlan()
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? string.Empty;

                Log.Information($"InsertScheduleToPlan start in site {thisSite}.");

                MakePlanForRoot(thisSite);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Insert schedule to Plan Error: {ex}");
                throw;
            }
        }

        public void MakePlanForRoot(string thisSite)
        {
            var context = new RootContext(this._configuration, this._encryption);

            var rootContext = context.Context;

            var listSchedules = rootContext.S_ScheduleShrinkLogs.Where(ptr => !ptr.IsDelete && ptr.Site == thisSite).ToList();

            var dbConnectionConfig = rootContext.D_DatabaseConnections.Where(ptr => !ptr.IsDelete).FirstOrDefault();

            if (dbConnectionConfig == null)
            {
                return;
            }

            foreach (var schedule in listSchedules)
            {
                switch (schedule.Type)
                {
                    case TypeScheduleBase.NumberOfHoursMinutes:
                        if (schedule.Hour == "00")
                        {
                            #region Mỗi ...phút một lần

                            EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                            {
                                ListCategory = schedule.ListCategory,
                                ScheduleId = schedule.Id,
                                Name = schedule.Name,
                                KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                                Site = schedule.Site,
                                SiteRunning = schedule.Site,
                                StatusPlan = StatusPlanShrinkLog.Await,
                                Type = schedule.Type,
                                DeploymentType = dbConnectionConfig.DeploymentType,
                                MethodShrinkLog = schedule.MethodShrinkLog,
                                FormatCron = $"0 0/{FormartTime(schedule.Minute)} * * * ?"
                            });
                            #endregion
                        }
                        else if (schedule.Minute == "00")
                        {
                            EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                            {
                                ListCategory = schedule.ListCategory,
                                ScheduleId = schedule.Id,
                                Name = schedule.Name,
                                Type = schedule.Type,
                                KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                                Site = schedule.Site,
                                SiteRunning = schedule.Site,
                                StatusPlan = StatusPlanShrinkLog.Await,
                                DeploymentType = dbConnectionConfig.DeploymentType,
                                MethodShrinkLog = schedule.MethodShrinkLog,
                                FormatCron = $"0 0 */{FormartTime(schedule.Hour ?? string.Empty)} ? * *"
                            });
                        }
                        else
                        {
                            #region Mỗi giờ(hai hoặc ba giờ,...) phút thứ... một lần

                            EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                            {
                                ListCategory = schedule.ListCategory,
                                ScheduleId = schedule.Id,
                                Name = schedule.Name,
                                Type = schedule.Type,
                                KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                                Site = schedule.Site,
                                SiteRunning = schedule.Site,
                                StatusPlan = StatusPlanShrinkLog.Await,
                                DeploymentType = dbConnectionConfig.DeploymentType,
                                MethodShrinkLog = schedule.MethodShrinkLog,
                                FormatCron = $"0 {FormartTime(schedule.Minute)} */{FormartTime(schedule.Hour ?? string.Empty)} ? * *"
                            });
                            #endregion
                        }
                        break;
                    case TypeScheduleBase.Daily:
                        #region Hàng ngày vào giờ... phút...
                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 {schedule.Minute} {schedule.Hour} * * ? *"
                        });
                        break;
                    #endregion
                    case TypeScheduleBase.Weekly:
                        #region Hàng tuần vào thứ... giờ... phút...
                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 {schedule.Minute} {schedule.Hour} ? * {schedule.Weekday}"
                        });
                        break;
                    #endregion
                    case TypeScheduleBase.Monthly:
                        #region Hàng tháng vào ngày... giờ... phút...
                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 {schedule.Minute} {schedule.Hour} {schedule.Day} * ?"
                        });
                        break;
                    #endregion
                    default:
                        Log.Error($"Not found type schedule {schedule.Id}");
                        throw new IboxException($"Not found type {schedule.Type} schedule {schedule.Id}");
                }
            }

            rootContext.Context.Dispose();
        }

        public void MakePlanForRoot(string scheduleId, string thisSite)
        {
            var context = new RootContext(this._configuration, this._encryption);

            var rootContext = context.Context;

            var schedule = rootContext.S_ScheduleShrinkLogs.Where(ptr => !ptr.IsDelete && ptr.Site == thisSite && ptr.Id == scheduleId).FirstOrDefault();

            var dbConnectionConfig = rootContext.D_DatabaseConnections.Where(ptr => !ptr.IsDelete).FirstOrDefault();

            if (dbConnectionConfig == null)
            {
                return;
            }

            if (schedule == null)
            {
                Log.Error($"Not found schedule {scheduleId}");

                return;
            }

            switch (schedule.Type)
            {
                case TypeScheduleBase.NumberOfHoursMinutes:
                    if (schedule.Hour == "00")
                    {
                        #region Mỗi ...phút một lần

                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 0/{FormartTime(schedule.Minute)} * * * ?"
                        });
                        #endregion
                    }
                    else if (schedule.Minute == "00")
                    {
                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 0 */{FormartTime(schedule.Hour ?? string.Empty)} ? * *"
                        });
                    }
                    else
                    {
                        #region Mỗi giờ(hai hoặc ba giờ,...) phút thứ... một lần

                        EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                        {
                            ListCategory = schedule.ListCategory,
                            ScheduleId = schedule.Id,
                            Name = schedule.Name,
                            Type = schedule.Type,
                            KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                            Site = schedule.Site,
                            SiteRunning = schedule.Site,
                            StatusPlan = StatusPlanShrinkLog.Await,
                            DeploymentType = dbConnectionConfig.DeploymentType,
                            MethodShrinkLog = schedule.MethodShrinkLog,
                            FormatCron = $"0 {FormartTime(schedule.Minute)} */{FormartTime(schedule.Hour ?? string.Empty)} ? * *"
                        });
                        #endregion
                    }
                    break;
                case TypeScheduleBase.Daily:
                    #region Hàng ngày vào giờ... phút...
                    EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                    {
                        ListCategory = schedule.ListCategory,
                        ScheduleId = schedule.Id,
                        Name = schedule.Name,
                        Type = schedule.Type,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        StatusPlan = StatusPlanShrinkLog.Await,
                        DeploymentType = dbConnectionConfig.DeploymentType,
                        MethodShrinkLog = schedule.MethodShrinkLog,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} * * ? *"
                    });

                    break;
                #endregion
                case TypeScheduleBase.Weekly:
                    #region Hàng tuần vào thứ... giờ... phút...
                    EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                    {
                        ListCategory = schedule.ListCategory,
                        ScheduleId = schedule.Id,
                        Name = schedule.Name,
                        Type = schedule.Type,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        StatusPlan = StatusPlanShrinkLog.Await,
                        DeploymentType = dbConnectionConfig.DeploymentType,
                        MethodShrinkLog = schedule.MethodShrinkLog,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} ? * {schedule.Weekday}"
                    });
                    break;
                #endregion
                case TypeScheduleBase.Monthly:
                    #region Hàng tháng vào ngày... giờ... phút...
                    EntityAction.TryCreate<S_PlanShrinkLog>(null, rootContext, new S_PlanShrinkLog()
                    {
                        ListCategory = schedule.ListCategory,
                        ScheduleId = schedule.Id,
                        Name = schedule.Name,
                        Type = schedule.Type,
                        KeyTrigg = $"Job_{schedule.Id}_{schedule.Site}",
                        Site = schedule.Site,
                        SiteRunning = schedule.Site,
                        StatusPlan = StatusPlanShrinkLog.Await,
                        DeploymentType = dbConnectionConfig.DeploymentType,
                        MethodShrinkLog = schedule.MethodShrinkLog,
                        FormatCron = $"0 {schedule.Minute} {schedule.Hour} {schedule.Day} * ?"
                    });
                    break;
                #endregion
                default:
                    Log.Error($"Not found type schedule {schedule.Id}");
                    throw new IboxException($"Not found type {schedule.Type} schedule {schedule.Id}");
            }

            rootContext.Context.Dispose();
        }

        /// <summary>
		/// xử lý định dạng thời gian
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		private string FormartTime(string time)
        {
            if (time.StartsWith('0'))
            {
                return time.Remove(0, 1);
            }

            return time;
        }

        /// <summary>
        /// Lấy danh sách Job đang chạy hoặc đã stop trên schedule trong ngày
        /// </summary>
        /// <param name="reqAllPlansDuringDay"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public ResAllPlansDuringDay GetAllPlansDuringDay(ReqAllPlansDuringDay reqAllPlansDuringDay)
        {
            try
            {
                var rootContext = new RootContext(this._configuration, this._encryption).Context;

                var listPlans = rootContext.S_PlanShrinkLogs.Where(ptr =>
                                                            !ptr.IsDelete
                                                            && ptr.CreatedDate != null
                                                            && ptr.CreatedDate.Value.Date == DateTime.Now.Date)
                                                            .OrderByDescending(ptr => ptr.CreatedDate);

                if (reqAllPlansDuringDay.PageNum == -1)
                {
                    var result = new ResAllPlansDuringDay()
                    {
                        Data = listPlans.ToList(),
                        TotalReCords = listPlans.Count()
                    };

                    rootContext.Dispose();

                    return result;

                }
                else
                {
                    int numberOfObjectsPerPage = 30;
                    var queryPage = listPlans
                        .Skip(numberOfObjectsPerPage * (reqAllPlansDuringDay.PageNum - 1))
                        .Take(numberOfObjectsPerPage)
                        .ToList();

                    var result = new ResAllPlansDuringDay()
                    {
                        Data = queryPage,
                        TotalReCords = listPlans.Count()
                    };

                    rootContext.Dispose();

                    return result;

                }
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllPlansDuringDay error: {ex}");
                throw new IboxException($"An unexpected error has occurred: {ex.Message}", ex);
            }
        }
    }
}
