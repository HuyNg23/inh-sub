using Microsoft.AspNetCore.Http;

namespace IBox.Database.Tenant
{
    public interface IIBGlobalTenantConfig
    {
        List<IBGlobalConfigList> GetAllConfiguration(HttpRequest requestContext);
        void OnLoadConfigChatBot();
    }
}