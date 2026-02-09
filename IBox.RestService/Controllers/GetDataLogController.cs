using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.FolderLog;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text;

namespace IBox.RestService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GetDataLogController : Controller
    {
        private readonly IRestLog _restLog;

        public GetDataLogController(IRestLog restLog)
        {
            _restLog = restLog;
        }

        [HttpGet("GetDataStream")]
        public async Task GetDataStream()
       => await _restLog.StreamDataLogCache(Response, HttpContext);

    }
}