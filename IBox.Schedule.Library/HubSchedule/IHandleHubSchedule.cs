using IBox.Common.Model;

namespace IBox.Schedule.Library.HubSchedule
{
    public interface IHandleHubSchedule
    {
        Task CreateHubSchedule(TypeUserBase typeUser);

        bool CheckHubReconnected(string urlHub);
    }
}