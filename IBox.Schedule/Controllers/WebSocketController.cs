using IBox.Schedule.WebSocket;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Schedule.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly IConnectWebSocket _connectWebSocket;

        public WebSocketController(IConnectWebSocket connectWebSocket)
        {
            _connectWebSocket = connectWebSocket;
        }

        [HttpGet]
        public string GetSocket()
        {
            _connectWebSocket.CreateWebSocket("", "", 1234, "");
            return string.Empty;
        }
    }
}