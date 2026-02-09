using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.LogService.Controller.ChatBot
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    public class DataChatBotController : ControllerBase
    {
        public readonly ICommonData _commonData;

        public DataChatBotController(ICommonData commonData)
        {
            _commonData = commonData;
        }

        [HttpGet("Download/{filename}/{date}")]
        public IActionResult DownloadFromLocal(string fileName, string date)
        {
            DateTime dateTime = _commonData.ToDate1(date);
            string tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            string pathCurrent = Path.Combine(DataPath.DataBaseChatBot, tenantId);
            string filePathToday = Path.Combine(Directory.GetCurrentDirectory(), _commonData.GetFilePath(pathCurrent, dateTime), "chatbot.db");
            FileStream reader;
            reader = new FileStream(filePathToday, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileName = "chatbot.db";

            return File(reader, "application/force-download", fileName);
        }
    }
}