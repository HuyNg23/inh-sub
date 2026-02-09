using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.DynamicLinkedLibrary
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLLController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBContext<TenantContext> _tenantContext;

        public DLLController(IServiceProvider serviceProvider, IBContext<TenantContext> tenantContext)
        {
            _serviceProvider = serviceProvider;
            _tenantContext = tenantContext;
        }

        [HttpPost("UploadDLL")]
        [IBoxAuthorization]
        [IBoxActionPermission("integration-config-library-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> UploadDLL([FromForm] RequestForm<BDynamicLinkedLibrary> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(req.Body.Name?.Trim()))
                {
                    throw new IboxLog("Name can't null or empty.", tenantID);
                }

                return req.Body.SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).SaveFile(HttpContext.Request);
            });
        }

        [HttpPost("UpdateDLLConfig")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-library-manage")]
        public ResponseForm<dynamic> UpdateDLLConfig(RequestForm<BDynamicLinkedLibrary> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(req.Body.Name?.Trim()))
                {
                    throw new IboxLog("Name can't null or empty.", tenantID);
                }

                req.Body.SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).UpdateDllConfig();
                return string.Empty;
            });
        }

        [HttpPost("GetAll")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-library-manage", "integration-config-library-view")]
        public ResponseForm<dynamic> GetAll(RequestForm<BDynamicLinkedLibrary> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return req.Body.SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).GetAllDll();
            });
        }

        [HttpPost("GetDetail")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-library-manage", "integration-config-library-view")]
        public ResponseForm<dynamic> GetDetail(RequestForm<BDynamicLinkedLibrary> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return req.Body.SetTenantContext(_tenantContext, tenantID).Init(_serviceProvider).GetDetail();
            });
        }

        [HttpPost("UploadFile")]
        [IBoxAuthorization]
        [IBoxPermissions]
        [IBoxActionPermission("integration-config-library-manage")]
        public ResponseForm<dynamic> SyncFileHA(IFormFile file)
        {
            return new ResponseForm<dynamic>(() =>
            {
                new BDynamicLinkedLibrary().SaveFile(file);
            });
        }

        [HttpPost("RootRequestSyncFileHA")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> RootRequestSyncFileHA(IFormFile file)
        {
            return new ResponseForm<dynamic>(() =>
            {
                new BDynamicLinkedLibrary().Init(_serviceProvider).SaveFile(file);
            });
        }

        [HttpPost("SyncFileHA")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> SyncFileHA()
        {
            return new ResponseForm<dynamic>(() =>
            {
                new BDynamicLinkedLibrary().Init(_serviceProvider).SyncFileOnRing(HttpContext.Request);
            });
        }

        [HttpPost("CallSyncFileHA")]
        [IBoxRootAuthorization]
        public ResponseForm<dynamic> CallSyncFileHA()
        {
            return new ResponseForm<dynamic>(() =>
            {
                new BDynamicLinkedLibrary().Init(_serviceProvider).CallSyncFileOnRing(HttpContext.Request);
            });
        }

        [HttpGet("DownloadDLL/{fileName}")]
        [IBoxAuthorization]
        public IActionResult Download(string fileName)
        {
            byte[] fileBytes = System.IO.File.ReadAllBytes(Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}DLL{Path.DirectorySeparatorChar}" + fileName + ".dll");

            return File(fileBytes, "application/force-download", fileName);
        }

        [HttpGet("CheckFileExist")]
        public IActionResult CheckFileExist(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "DLL", fileName);
            return Ok(System.IO.File.Exists(filePath));
        }

        [HttpGet("DownloadFile")]
        public IActionResult DownloadFile(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "DLL", fileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(stream, "application/octet-stream", fileName);
        }
    }
}