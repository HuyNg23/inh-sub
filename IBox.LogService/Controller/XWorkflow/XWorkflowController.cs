using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx.Execution;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.LogService.Controllers.XWorkflow
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class XWorkflowController : ControllerBase
    {
        private readonly IExecuteWF executeWF;
        private readonly IBContext<TenantContext> _tenantContext;

        public XWorkflowController(
            IBContext<TenantContext> tenantContext,
            IExecuteWF executeWF)
        {
            this._tenantContext = tenantContext;
            this.executeWF = executeWF;
        }

        [HttpPost]
        [Route("Debug/{tenantId}/{id}")]
        public ResponseForm<dynamic> Debug(string tenantId, string id, RequestForm<dynamic> req)
        {
            try
            {
                if (!Request.Body.CanSeek)
                {
                    Request.EnableBuffering();
                }
                var result = this.executeWF.SetTenantContext(_tenantContext, tenantId).AddHeader(this.Request.Headers).ExecuteDebug(id, req.Body, tenantId);

                return new ResponseForm<dynamic>(() => result.Debug);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    ex.Message
                });
            }
        }

        /// <summary>
        /// Debug workflow sử dụng cho page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DebugWorkflow/{tenantId}/{id}")]
        public ResponseForm<dynamic> DebugWorkflow(string tenantId, string id, RequestForm<dynamic> req)
        {
            try
            {
                if (!Request.Body.CanSeek)
                {
                    Request.EnableBuffering();
                }

                var result = this.executeWF.SetTenantContext(_tenantContext, tenantId).AddHeader(this.Request.Headers).ExecuteDebug(id, req.Body, tenantId);

                return new ResponseForm<dynamic>(() => result.DebugResult);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => new
                {
                    Code = "1",
                    ex.Message
                });
            }
        }
    }
}