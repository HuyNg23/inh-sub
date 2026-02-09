using IBox.Common.Objects;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.StoreService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetLibraryController : ControllerBase
    {
        private readonly IUpLoadAssetLibrary _upLoadAssetLibrary;

        public AssetLibraryController(IUpLoadAssetLibrary upLoadAssetLibrary)
        {
            _upLoadAssetLibrary = upLoadAssetLibrary;
        }

        [HttpPost("UploadAssetLibrary")]
        [IBoxAuthorization]
        [IBoxActionPermission("integration-config-page-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> UploadAssets([FromForm] RequestForm<ReqUpLoadAssetLibrary> req)
        {
            var authorization = Request.Headers["Authorization"].ToString();
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(req.Body.Name))
            {
                throw new IboxLog("Name can't null or empty.", "AppLogs");
            }

            if (string.IsNullOrEmpty(req.Body.Version))
            {
                throw new IboxLog("Version can't null or empty.", "AppLogs");
            }

            return new ResponseForm<dynamic>(() =>
            {
                return _upLoadAssetLibrary.UpLoadFileAssetLibrary(req.Body, tenantId, authorization);
            });
        }

        [HttpPost("SaveFileAssetLibrary")]
        [IBoxAuthorization]
        //[IBoxActionPermission("integration-config-page-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> SaveFileAssetLibrary([FromForm] ReqUpLoadAssetLibrary req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(req.Name))
            {
                throw new IboxLog("Name can't null or empty.", "AppLogs");
            }

            if (string.IsNullOrEmpty(req.Version))
            {
                throw new IboxLog("Version can't null or empty.", "AppLogs");
            }

            return new ResponseForm<dynamic>(() =>
            {
                return _upLoadAssetLibrary.SaveFileAssetLibrary(req, tenantId);
            });
        }

        [HttpPost("DeleteFileOnLocal")]
        [IBoxAuthorization]
        //[IBoxActionPermission("integration-config-page-manage")]
        [RequestSizeLimit(8388608)]
        public ResponseForm<dynamic> DeleteFileOnLocal(ReqUpLoadAssetLibrary req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            return new ResponseForm<dynamic>(() =>
            {
                return _upLoadAssetLibrary.DeleteFileOnLocal(req.Path, tenantId);
            });
        }

        [HttpPost("SaveFileHTML")]
        [IBoxAuthorization]
        public ResponseForm<dynamic> SaveFileHTML(ReqWebResource req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            return new ResponseForm<dynamic>(() =>
            {
                return _upLoadAssetLibrary.SaveFileHTML(req, tenantId);
            });
        }

        [HttpPost("UrlGetFileHTML")]
        [IBoxAuthorization]
        public async Task<ResWebResource> UrlGetFileHTML(ReqWebResource req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return await _upLoadAssetLibrary.UrlGetFileHTML(req, tenantId);
        }
    }
}
