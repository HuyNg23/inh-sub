using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Documentation.Workflow;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IBox.StoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class StoreController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBContext<TenantContext> _tenantContext;

        public StoreController(IServiceProvider serviceProvider, IBContext<TenantContext> tenantContext)
        {
            _serviceProvider = serviceProvider;
            _tenantContext = tenantContext;
        }

        [HttpGet("Backup/{wfid}")]
        [IBoxActionPermission("integration-config-workflow-manage")]
        public IActionResult Backup(string wfid)
        {
            var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            var content = new Expose().SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).BackupFile(wfid);
            List<LogInfo> logInfos = new List<LogInfo>();
            _tenantContext.Context.Dispose();
            var zipName = string.Format("{0}{1}.bak", tenantID, wfid);
            return File(Encoding.UTF8.GetBytes(content), "text/plain", zipName);
        }

        [HttpPost("Restore")]
        [RequestSizeLimit(8388608)]
        [IBoxActionPermission("integration-config-workflow-manage")]
        public ResponseForm<dynamic> Restore([FromForm] RequestForm<BDynamicLinkedLibrary> reqX)
        {
            if (reqX.Body.File?.Length > 8388608)
            {
                throw new IboxLog("File size exceeds the 8MB limit.", "AppLogs");
            }

            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                new Expose().SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).Restore(reqX.Body.File);

                return Ok();
            });
        }
    }
}