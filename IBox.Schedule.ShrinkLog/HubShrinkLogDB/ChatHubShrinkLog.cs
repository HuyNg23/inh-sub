using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace IBox.Schedule.ShrinkLog.HubShrinkLogDB
{
    public class ChatHubShrinkLog : Hub
    {
        public async Task SendMessage(string message)
        {
            Log.Information($"Received message: {message}");
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
