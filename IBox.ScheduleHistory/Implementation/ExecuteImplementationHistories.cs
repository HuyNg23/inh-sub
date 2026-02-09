using IBox.Common.Objects;
using IBox.Common.ServiceIB;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.ScheduleHistory.Execution;
using IBox.ScheduleHistory.Model;
using Newtonsoft.Json;
using Serilog;
using System.Data.Entity;

namespace IBox.ScheduleHistory.Implementation
{
    public class ExecuteImplementationHistories : ATenantContext<ExecuteImplementationHistories>, IExecuteImplementationHistories
    {
        private readonly IBContext<TenantContext> _tenantContext;
        private readonly IRestAPI _restAPI;

        public ExecuteImplementationHistories(IBContext<TenantContext> tenantContext, IRestAPI restAPI)
        {
            _tenantContext = tenantContext;
            _restAPI = restAPI;
        }

        /// <summary>
        /// Lấy danh sách lịch sử chạy schedule theo ngày
        /// </summary>
        /// <param name="getImPlanHistory"></param>
        /// <param name="tennantID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<ScheduleHistoryLineTime> GetScheduleHistoryLineTimeDay(GetImPlanHistory getImPlanHistory, string tennantID)
        {
            if (getImPlanHistory == null || string.IsNullOrEmpty(getImPlanHistory.SelectDate))
            {
                throw new IboxLog("Request or selectDate is null or empty.", tennantID);
            }

            DateTime selectDate = DateTime.Parse(getImPlanHistory.SelectDate, System.Globalization.CultureInfo.InvariantCulture);
            var startOfDay = selectDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var summaryDict = Enumerable.Range(0, 24)
                .Select(hour => new ScheduleHistoryLineTime
                {
                    Hour = hour,
                    CountSuccess = 0,
                    CountFail = 0
                })
                .ToDictionary(s => s.Hour);

#pragma warning disable CS8629 // Nullable value type may be null.
            var listScheduleHistoryDay = _tenantContext.GetTenantContext(tennantID).Context.S_ImplementationHistorys
                .Where(ptr =>
                    (ptr.StatusPlan == StatusPlan.Fail || ptr.StatusPlan == StatusPlan.Complete)
                    && (ptr.Site == getImPlanHistory.Site || string.IsNullOrEmpty(getImPlanHistory.Site))
                    && ptr.CreatedDate >= startOfDay && ptr.CreatedDate <= endOfDay
                    )
                .GroupBy(history => new
                {
                    Hour = history.CreatedDate.Value.Hour,
                    StatusPlan = history.StatusPlan
                })
                .Select(group => new
                {
                    Hour = group.Key.Hour,
                    StatusPlan = group.Key.StatusPlan,
                    Count = group.Count()
                })
                .ToList();
#pragma warning restore CS8629 // Nullable value type may be null.

            foreach (var entry in listScheduleHistoryDay)
            {
                if (summaryDict.TryGetValue(entry.Hour, out var summary))
                {
                    if (entry.StatusPlan == StatusPlan.Complete)
                    {
                        summary.CountSuccess = entry.Count;
                    }
                    else if (entry.StatusPlan == StatusPlan.Fail)
                    {
                        summary.CountFail = entry.Count;
                    }
                }
            }

            return summaryDict.Values.ToList();
        }

        /// <summary>
        /// Lấy danh sách lịch sử chạy schedule theo tháng
        /// </summary>
        /// <param name="getImPlanHistory"></param>
        /// <param name="tennantID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<ScheduleHistoryLineTimeMonth> GetScheduleHistoryLineTimeMonth(GetImPlanHistory getImPlanHistory, string tennantID)
        {
            if (getImPlanHistory == null || string.IsNullOrEmpty(getImPlanHistory.SelectDate))
            {
                throw new IboxLog("Request or selectDate isNullOrEmpty.", tennantID);
            }

            DateTime selectDate = DateTime.Parse(getImPlanHistory.SelectDate, System.Globalization.CultureInfo.InvariantCulture);

            var startOfMonth = new DateTime(selectDate.Year, selectDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
            var summaryList = Enumerable.Range(1, DateTime.DaysInMonth(selectDate.Year, selectDate.Month))
                .Select(day => new ScheduleHistoryLineTimeMonth
                {
                    Date = day.ToString("00"),
                    CountSuccess = 0,
                    CountFail = 0
                })
                .ToDictionary(s => s.Date);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

            var result = _tenantContext.GetTenantContext(tennantID).Context.S_ImplementationHistorys
                .Where(ptr =>
                    (ptr.StatusPlan == StatusPlan.Complete || ptr.StatusPlan == StatusPlan.Fail)
                    && ptr.CreatedDate >= startOfMonth && ptr.CreatedDate <= endOfMonth
                    && (ptr.Site == getImPlanHistory.Site || string.IsNullOrEmpty(getImPlanHistory.Site))
                    )
                .GroupBy(ptr => new { Date = ptr.CreatedDate!.Value.Day, StatusPlan = ptr.StatusPlan })
                .Select(group => new
                {
                    Day = group.Key.Date,
                    StatusPlan = group.Key.StatusPlan,
                    Count = group.Count()
                })
                .ToList();

            foreach (var entry in result)
            {
                string formattedDay = entry.Day.ToString("00");
                if (summaryList.TryGetValue(formattedDay, out var summary))
                {
                    if (entry.StatusPlan == StatusPlan.Complete)
                    {
                        summary.CountSuccess = entry.Count;
                    }
                    else if (entry.StatusPlan == StatusPlan.Fail)
                    {
                        summary.CountFail = entry.Count;
                    }
                }
            }

            return summaryList.Values.ToList();
        }

        /// <summary>
        /// Tổng hợp lịch sử kết quả chạy schedule thành công/thất bại
        /// </summary>
        /// <param name="tenantID"></param>
        /// <returns></returns>
        public SumScheduleImplementationHistory SumScheduleImplementationHistories(string tenantID)
        {
            var tenantContext = _tenantContext.GetTenantContext(tenantID).Context;
            var sumSuccess = tenantContext.S_ImplementationHistorys.Where(ptr => ptr.StatusPlan == StatusPlan.Complete && !ptr.IsDelete).Count();
            var sumFail = tenantContext.S_ImplementationHistorys.Where(ptr => ptr.StatusPlan == StatusPlan.Fail && !ptr.IsDelete).Count();

            return new SumScheduleImplementationHistory()
            {
                CountSuccess = sumSuccess,
                CountFail = sumFail
            };
        }

        /// <summary>
        /// Tổng hợp chi tiết lịch sử chạy schedule
        /// </summary>
        /// <param name="tenantID"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        public ResponseAllScheduleHistory AllScheduleHistories(string tenantID, RequestAllScheduleHistory requestAllScheduleHistory)
        {
            var tenantContext = _tenantContext.GetTenantContext(tenantID);
            var serverSites = IBGlobalConfig.Services.Where(ptr => ptr.TypeService == Database.Root.Tables.TypeService.ScheduleService).ToList();

            DateTime startDate;
            DateTime endDate;

            if (string.IsNullOrEmpty(requestAllScheduleHistory.StartDate) || string.IsNullOrEmpty(requestAllScheduleHistory.EndDate))
            {
                startDate = DateTime.Now;
                endDate = DateTime.Now;
                startDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Utc);
                endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59, DateTimeKind.Utc);
            }
            else
            {
                startDate = DateTime.ParseExact(requestAllScheduleHistory.StartDate, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                endDate = DateTime.ParseExact(requestAllScheduleHistory.EndDate, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            }

            int PageNum = 30;
            int CurrentRow = (requestAllScheduleHistory.PageNum - 1) * PageNum;
            IQueryable<AllScheduleHistory> query;
            int totalRecords = 0;
            StatusPlan statusPlan = new StatusPlan();
            switch (requestAllScheduleHistory.StatusSearch)
            {
                case StatusSearch.Failed:
                    statusPlan = StatusPlan.Fail;
                    break;

                case StatusSearch.Success:
                    statusPlan = StatusPlan.Complete;
                    break;

                default:
                    break;
            }

            if (StatusSearch.None == requestAllScheduleHistory.StatusSearch)
            {



                query = (from g in tenantContext.Context.S_ImplementationHistorys
                         join s in tenantContext.Context.S_Schedules on g.ScheduleID equals s.Id into scheduleJoin
                         from schedule in scheduleJoin.DefaultIfEmpty()
                         join wf in tenantContext.Context.WF_Defines on schedule.Wfid equals wf.Id into workflowJoin
                         from workflow in workflowJoin.DefaultIfEmpty()
                         where g.CreatedDate >= startDate && g.CreatedDate <= endDate &&
                        (string.IsNullOrEmpty(requestAllScheduleHistory.TextSearch)
                          || schedule.Name.Contains(requestAllScheduleHistory.TextSearch)
                          || workflow.Name.Contains(requestAllScheduleHistory.TextSearch)
                          || g.Site.Contains(requestAllScheduleHistory.TextSearch))
                         orderby g.CreatedDate descending
                         select new AllScheduleHistory()
                         {
                             KeyExecuteRunWorkFlow = g.KeyExecuteRunWorkFlow,
                             Site = g.Site,
                             Status = g.StatusPlan,
                             Type = g.Type,
                             MinTimeStart = g.CreatedDate,
                             MaxTimeStart = g.ModificationDate,
                             NameSchedule = schedule.Name ?? "",
                             NameWf = workflow.Name ?? "",
                             TimeStart = g.CreatedDate,
                             TimeRun = g.ModificationDate - g.CreatedDate,
                             Reason = g.Reason
                         }).Skip(CurrentRow).Take(PageNum);






                totalRecords = (from g in tenantContext.Context.S_ImplementationHistorys
                                join s in tenantContext.Context.S_Schedules on g.ScheduleID equals s.Id into scheduleJoin
                                from schedule in scheduleJoin.DefaultIfEmpty()
                                join wf in tenantContext.Context.WF_Defines on schedule.Wfid equals wf.Id into workflowJoin
                                from workflow in workflowJoin.DefaultIfEmpty()
                                where g.CreatedDate >= startDate && g.CreatedDate <= endDate &&
                               (string.IsNullOrEmpty(requestAllScheduleHistory.TextSearch)
                                 || schedule.Name.Contains(requestAllScheduleHistory.TextSearch)
                                 || workflow.Name.Contains(requestAllScheduleHistory.TextSearch)
                                 || g.Site.Contains(requestAllScheduleHistory.TextSearch))
                                select g).Count();



            }
            else
            {



                query = (from g in tenantContext.Context.S_ImplementationHistorys
                         join s in tenantContext.Context.S_Schedules on g.ScheduleID equals s.Id into scheduleJoin
                         from schedule in scheduleJoin.DefaultIfEmpty()
                         join wf in tenantContext.Context.WF_Defines on schedule.Wfid equals wf.Id into workflowJoin
                         from workflow in workflowJoin.DefaultIfEmpty()
                         where g.CreatedDate >= startDate && g.CreatedDate <= endDate && g.StatusPlan == statusPlan &&
                        (string.IsNullOrEmpty(requestAllScheduleHistory.TextSearch)
                          || schedule.Name.Contains(requestAllScheduleHistory.TextSearch)
                          || workflow.Name.Contains(requestAllScheduleHistory.TextSearch)
                          || g.Site.Contains(requestAllScheduleHistory.TextSearch))
                         orderby g.CreatedDate descending
                         select new AllScheduleHistory()
                         {
                             KeyExecuteRunWorkFlow = g.KeyExecuteRunWorkFlow,
                             Site = g.Site,
                             Status = g.StatusPlan,
                             Type = g.Type,
                             MinTimeStart = g.CreatedDate,
                             MaxTimeStart = g.ModificationDate,
                             NameSchedule = schedule.Name ?? "",
                             NameWf = workflow.Name ?? "",
                             TimeStart = g.CreatedDate,
                             TimeRun = g.ModificationDate - g.CreatedDate,
                             Reason = g.Reason
                         }).Skip(CurrentRow).Take(PageNum);






                totalRecords = (from g in tenantContext.Context.S_ImplementationHistorys
                                join s in tenantContext.Context.S_Schedules on g.ScheduleID equals s.Id into scheduleJoin
                                from schedule in scheduleJoin.DefaultIfEmpty()
                                join wf in tenantContext.Context.WF_Defines on schedule.Wfid equals wf.Id into workflowJoin
                                from workflow in workflowJoin.DefaultIfEmpty()
                                where g.CreatedDate >= startDate && g.CreatedDate <= endDate && g.StatusPlan == statusPlan &&
                               (string.IsNullOrEmpty(requestAllScheduleHistory.TextSearch)
                                 || schedule.Name.Contains(requestAllScheduleHistory.TextSearch)
                                 || workflow.Name.Contains(requestAllScheduleHistory.TextSearch)
                                 || g.Site.Contains(requestAllScheduleHistory.TextSearch))
                                select g).Count();



            }
            var a = query.ToList();
            return new ResponseAllScheduleHistory()
            {
                Data = query.ToList(),
                TotalReCords = totalRecords
            };
        }

        public SummaryMaxMinAvg GetSummaryMaxMinAvg(string tenantId, string selectedDate)
        {
            try
            {
                DateTime selectDate = DateTime.Parse(selectedDate, System.Globalization.CultureInfo.InvariantCulture);

                var startDate = selectDate.Date;
                var endDate = startDate.AddDays(1);

                var tenantContext = _tenantContext.GetTenantContext(tenantId).Context;

                var queryResult = tenantContext.S_ImplementationHistorys
                    .Where(h => h.CreatedDate >= startDate && h.CreatedDate < endDate && !h.IsDelete)
                    .GroupBy(h => new { h.CreatedDate!.Value.Year, h.CreatedDate!.Value.Month, h.CreatedDate!.Value.Day, h.CreatedDate!.Value.Hour, h.CreatedDate!.Value.Minute }) // Nhóm theo năm, tháng, ngày, giờ và phút
                    .Select(g => new
                    {
                        Time = $"{g.Key.Hour:D2}:{g.Key.Minute:D2}",
                        Count = g.Count()
                    })
                    .ToList();

                var maxRecord = queryResult.OrderByDescending(x => x.Count).FirstOrDefault();
                var minRecord = queryResult.OrderBy(x => x.Count).FirstOrDefault();

                var avgCount = queryResult.Any() ? (int)queryResult.Average(x => x.Count) : 0;

                // Tạo đối tượng SummaryMaxMinAvg
                var summary = new SummaryMaxMinAvg
                {
                    SumaryMax = maxRecord != null ? new SumaryMax
                    {
                        TimeMax = maxRecord.Time,
                        CountMax = maxRecord.Count
                    } : new SumaryMax(),
                    SumaryMin = minRecord != null ? new SumaryMin
                    {
                        TimeMin = minRecord.Time,
                        CountMin = minRecord.Count
                    } : new SumaryMin(),
                    AvgCount = avgCount
                };

                return summary;
            }
            catch (Exception ex)
            {
                throw new IboxLog(ex.Message, tenantId, ex);
            }
        }

        public SummaryMaxMinAvg GetSummaryMaxMinAvgMonth(string tenantId, string selectedMonth)
        {
            try
            {
                DateTime selectMonth = DateTime.Parse(selectedMonth, System.Globalization.CultureInfo.InvariantCulture);

                var startDate = new DateTime(selectMonth.Year, selectMonth.Month, 1);
                var endDate = startDate.AddMonths(1);

                var tenantContext = _tenantContext.GetTenantContext(tenantId).Context;

                var queryResult = tenantContext.S_ImplementationHistorys
                    .Where(h => h.CreatedDate >= startDate && h.CreatedDate < endDate && !h.IsDelete)
                    .GroupBy(h => h.CreatedDate!.Value.Day)
                    .Select(g => new
                    {
                        Day = g.Key,
                        Count = g.Count()
                    })
                    .ToList();

                var maxRecord = queryResult.OrderByDescending(x => x.Count).FirstOrDefault();
                var minRecord = queryResult.OrderBy(x => x.Count).FirstOrDefault();

                var avgCount = queryResult.Any() ? (int)queryResult.Average(x => x.Count) : 0;

                var summary = new SummaryMaxMinAvg
                {
                    SumaryMax = maxRecord != null ? new SumaryMax
                    {
                        TimeMax = $"{maxRecord.Day:D2}",
                        CountMax = maxRecord.Count
                    } : new SumaryMax(),

                    SumaryMin = minRecord != null ? new SumaryMin
                    {
                        TimeMin = $"{minRecord.Day:D2}",
                        CountMin = minRecord.Count
                    } : new SumaryMin(),

                    AvgCount = avgCount
                };

                return summary;
            }
            catch (Exception ex)
            {
                throw new IboxLog(ex.Message, tenantId, ex);
            }
        }

        public void CallAPIDeleteJobSchedule(S_Schedule obj, string author)
        {
            try
            {
                foreach (var urlSchedule in IBGlobalConfig.ScheduleServiceIBox)
                {
                    string urlScheduleService = string.Format(@"{0}{1}",
                             urlSchedule,
                             UrlServiceIBConfig.UrlDeleteJobSchedule
                           );
                    _restAPI.Send(new RestAPIRequest()
                    {
                        Body = JsonConvert.SerializeObject(obj),
                        Headers = new List<RestAPIHeader>()
                          {
                              new RestAPIHeader()
                              {
                                    Label = "Content-Type",
                                    Value = "application/json"
                              },
                               new RestAPIHeader()
                              {
                                    Label = "Authorization",
                                    Value = author
                              }
                          },
                        Method = "POST",
                        Timeout = 30,
                        Url = urlScheduleService
                    }, "AppLogs");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CallAPIDeleteJobSchedule: {JsonConvert.SerializeObject(obj)} \n Error: {ex.Message}");
            }
        }
    }
}