using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using IBox.Common.Security;
using IBox.Database.Root;

namespace IBox.Client.Controllers.XPage
{
    [Route("api/[controller]")]
    [IBoxAuthorization]
    [ApiController]
    public class AssetLibraryController: ActionController<P_AssetLibrary, TenantContext>
    {
        private readonly IUpLoadAssetLibrary _upLoadAssetLibrary;
        private readonly IFileAssetLibraryBase _fileAssetLibraryBase;

        public AssetLibraryController(IUpLoadAssetLibrary upLoadAssetLibrary, IBContext<TenantContext> tenantContext, IEncryption encryption, IFileAssetLibraryBase fileAssetLibraryBase) : base(tenantContext, encryption)
        {
            _upLoadAssetLibrary = upLoadAssetLibrary;
            _fileAssetLibraryBase = fileAssetLibraryBase;
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

        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return new ResponseForm<List<dynamic>>(() =>
            {
                throw new IboxLog("Can not use API", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
            });
        }

        [HttpPost("UploadAssetLibrary")]
        [IBoxActionPermission("integration-config-page-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> UpLoadFileAssetLibrary([FromForm] RequestForm<ReqUpLoadAssetLibrary> req)
        {
            var authorization = Request.Headers["Authorization"].ToString();

            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            return new ResponseForm<dynamic>(() =>
            {
                return _fileAssetLibraryBase.UpLoadFileAssetLibrary(req.Body, tenantId, authorization);
            });
        }

        [HttpPost("SaveFileAssetLibrary")]
        //[IBoxActionPermission("integration-config-page-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> SaveFileAssetLibrary([FromForm] ReqUpLoadAssetLibrary req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(req.Name))
            {
                throw new IboxLog("Name can't null or empty.", tenantId ?? "AppLogs");
            }

            if (string.IsNullOrEmpty(req.Version))
            {
                throw new IboxLog("Version can't null or empty.", tenantId ?? "AppLogs");
            }

            return new ResponseForm<dynamic>(() =>
            {
                return _upLoadAssetLibrary.SaveFileAssetLibrary(req, tenantId);
            });
        }

        [HttpPost("DeleteAssetLibrary")]
        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public ResponseForm<dynamic> DeleteAssetLibrary(RequestForm<ReqUpLoadAssetLibrary> req)
        {
            var authorization = Request.Headers["Authorization"].ToString();
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            return new ResponseForm<dynamic>(() =>
            {
                return _fileAssetLibraryBase.DeleteFileAssetLibrary(tenantId, req.Body.Id, authorization);
            });
        }

        [HttpGet("Cdn/{fileName}")]
        public IActionResult GetFileAssetLibrary(ReqUpLoadAssetLibrary req)
        {
            try
            {
                var authorization = Request.Headers["Authorization"].ToString();
                var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                var fileData = _fileAssetLibraryBase.GetFileAssetLibrary(req.Path, tenantId, authorization);
                return File(fileData, "application/octet-stream", "file");
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

    }
}
