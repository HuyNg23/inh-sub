using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace IBox.Schedule.SelfService.HubScheduler
{
    public class ChatHubSchedule : Hub
    {
        public async Task SendMessage(string message)
        {
            Log.Information($"Received message: {message}");
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
