using IBox.Common.FolderLog;
using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.LogService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    //[IBoxRootAuthorization]
    [IBoxRootTenantAuthorization]
    public class LogController : ControllerBase
    {
        private readonly IRestLog _restLog;
        public LogController(IRestLog restLog)
        {
            _restLog = restLog;
        }

        [HttpGet("Download/{filename}")]
        public IActionResult DownloadFromLocal(string fileName)
        {
            FileStream reader = new FileStream(Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return File(reader, "application/force-download", fileName);
        }

        [HttpGet("ShowList")]
        public ResponseForm<List<LogInfo>> ListLog()
        {
            return new ResponseForm<List<LogInfo>>(() =>
            {
                return _restLog.ListLog("Log");
            });
        }
    }
}