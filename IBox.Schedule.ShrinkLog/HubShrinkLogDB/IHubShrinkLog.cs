namespace IBox.Schedule.ShrinkLog.HubShrinkLogDB
{
    public interface IHubShrinkLog
    {
        Task CreateHubSchedule();
        bool CheckHubReconnected(string urlHub);
    }
}
