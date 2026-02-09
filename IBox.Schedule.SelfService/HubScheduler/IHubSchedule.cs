namespace IBox.Schedule.SelfService.HubScheduler
{
    public interface IHubSchedule
    {
        Task CreateHubSchedule();
        bool CheckHubReconnected(string urlHub);
    }
}
