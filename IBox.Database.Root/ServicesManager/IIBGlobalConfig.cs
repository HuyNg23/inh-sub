using Microsoft.AspNetCore.Http;

namespace IBox.Database.Root
{
    public interface IIBGlobalConfig
    {
        List<IBGlobalConfigList> GetAllConfiguration(HttpRequest requestContext);

        void OnLoad(string serviceName = "");

        void LoadConfigRoot();

        void MailAlertRunService(string serviceName);

        void SendRequestReload(HttpRequest request);
        void LoadService();
    }
}