using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.ScheduleHistory.Execution;
using IBox.ScheduleHistory.Model;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Schedule
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class ScheduleHistoryController : ActionController<S_ImplementationHistory, TenantContext>
    {
        private readonly IExecuteImplementationHistories _implementationHistories;

        public ScheduleHistoryController(IBContext<TenantContext> tenantContext, IExecuteImplementationHistories implementationHistories, IEncryption encryption) : base(tenantContext, encryption)
        {
            _implementationHistories = implementationHistories;
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return new ResponseForm<List<dynamic>>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        /// <summary>
        /// Lấy danh sách lịch sử chạy schedule theo ngày
        /// </summary>
        /// <param name="getImPlanHistory"></param>
        /// <returns></returns>
        [HttpPost("GetImPlanHistoryDay")]
        [IBoxActionPermission("home-schedule-view", "integration-config-schedule-config-view")]
        public ResponseForm<List<ScheduleHistoryLineTime>> GetScheduleHistoryLineTimeDay(RequestForm<GetImPlanHistory> getImPlanHistory)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<List<ScheduleHistoryLineTime>>(() => _implementationHistories.GetScheduleHistoryLineTimeDay(getImPlanHistory.Body, tenantId));
        }

        /// <summary>
        /// Lấy danh sách lịch sử chạy schedule theo tháng
        /// </summary>
        /// <param name="getImPlanHistory"></param>
        /// <returns></returns>
        [HttpPost("GetImPlanHistoryMonth")]
        [IBoxActionPermission("home-schedule-view", "integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public ResponseForm<List<ScheduleHistoryLineTimeMonth>> GetImPlanHistoryMonth(RequestForm<GetImPlanHistory> getImPlanHistory)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<List<ScheduleHistoryLineTimeMonth>>(() => _implementationHistories.GetScheduleHistoryLineTimeMonth(getImPlanHistory.Body, tenantId));
        }

        /// <summary>
        /// Tổng hợp lịch sử kết quả chạy schedule thành công/thất bại
        /// </summary>
        /// <returns></returns>
        [HttpPost("SumScheduleImplementationHistories")]
        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public ResponseForm<SumScheduleImplementationHistory> SumScheduleImplementationHistories()
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<SumScheduleImplementationHistory>(() => _implementationHistories.SumScheduleImplementationHistories(tenantId));
        }

        /// <summary>
        /// Tổng hợp chi tiết lịch sử chạy schedule
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [HttpPost("AllScheduleHistories")]
        [IBoxActionPermission("integration-config-schedule-config-manage", "integration-config-schedule-config-view")]
        public ResponseForm<ResponseAllScheduleHistory> AllScheduleHistories(RequestForm<RequestAllScheduleHistory> requestAllScheduleHistory)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<ResponseAllScheduleHistory>(() => _implementationHistories.AllScheduleHistories(tenantId, requestAllScheduleHistory.Body));
        }

        [HttpPost("GetSummaryMaxMinAvg")]
        public ResponseForm<SummaryMaxMinAvg> GetSummaryMaxMinAvg(RequestForm<GetImPlanHistory> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<SummaryMaxMinAvg>(() => _implementationHistories.GetSummaryMaxMinAvg(tenantId, req.Body.SelectDate ?? string.Empty));
        }

        [HttpPost("GetSummaryMaxMinAvgMonth")]
        public ResponseForm<SummaryMaxMinAvg> GetSummaryMaxMinAvgMonth(RequestForm<GetImPlanHistory> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<SummaryMaxMinAvg>(() => _implementationHistories.GetSummaryMaxMinAvgMonth(tenantId, req.Body.SelectDate ?? string.Empty));
        }
    }
}