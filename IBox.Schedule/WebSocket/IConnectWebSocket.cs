namespace IBox.Schedule.WebSocket
{
    public interface IConnectWebSocket
    {
        Task CreateWebSocket(string tenantId, string domain, int port, string site);
    }
}