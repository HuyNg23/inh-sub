using IBox.Common.Model;

namespace IBox.Common.TCP
{
    public interface IRestAPI : IDisposable
    {
        RestAPIResponse Send(RestAPIRequest req, string tenantId);

        RestAPIResponse SendChatBot(RestAPIRequest req, string tenantId = "AppLogs");

        RestAPIResponse Send(RestAPIRequestWFConfig req, string tenantId);

        void SendMailAlert(List<string> LogServiceIBox, object obj);

        public void SendAsync(RestAPIRequestWFConfig req, string tenantId, Action<string, HttpResponseMessage>? functionCallbackSuccess = null, Action<string, HttpResponseMessage>? functionCallbackError = null, Action<string, string>? functionCallbackErrorTask = null);
    }
}