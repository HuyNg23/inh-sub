using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Serilog;

namespace IBox.Schedule.SelfService.HandleScheduleDaily
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
        /// Thực thi insert Schedule vào ImplementationPlan
        /// </summary>
        /// <returns></returns>
        public bool InsertScheduleToImplementationPlan()
        {
            try
            {
                string thisSite = _configuration.Config.Value.ThisSite ?? "";

                Log.Information($"InsertScheduleToImplementationPlan start in site {thisSite}.");

                var rootContext = new RootContext(this._configuration, this._encryption);

                var listTenant = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

                rootContext.Context.Dispose();

                for (int i = 0; i < listTenant.Count; i++)
                {
                    var tenant = listTenant[i];

                    var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

                    var tenantContext = context.GetTenantContext(tenant.Id).Context;

                    var listSchedule = tenantContext.S_Schedules.Where(ptr => !ptr.IsDelete && ptr.Site == thisSite).ToList();

                    tenantContext.Context.Dispose();

                    MakePlanForTenant(listSchedule, tenant.Id);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Insert schedule to ImplementationPlan Error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Định dạng loại Schedule rồi insert vào ImplementationPlan
        /// </summary>
        /// <param name="listSchedule"></param>
        /// <exception cref="Exception"></exception>
        void MakePlanForTenant(List<S_Schedule> listSchedule, string tenantId)
        {
            var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));

            var tenantContext = context.GetTenantContext(tenantId).Context;

            foreach (var schedule in listSchedule)
            {
                switch (schedule.Type)
                {
                    case TypeScheduleBase.NumberOfHoursMinutes:
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
                    case TypeScheduleBase.Daily:
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
                    case TypeScheduleBase.Weekly:
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
                    case TypeScheduleBase.Monthly:
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

            tenantContext.Context.Dispose();
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
    }
}
