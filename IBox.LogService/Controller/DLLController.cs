using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.LogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLLController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public DLLController(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        [HttpPost("UploadFile")]
        [IBoxAuthorization]
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
    }
}