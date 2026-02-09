using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.ChatBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DLLController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;

        public DLLController(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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