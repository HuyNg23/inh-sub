using IBox.Common.Objects;
using IBox.Common.TCP;
using Serilog;

namespace IBox.Common.Chat.CommonData.CallData
{
    public class CallData : ICallData
    {
        public readonly ICommonData _commonData;
        public readonly IRestAPI _restAPI;

        public CallData(ICommonData commonData, IRestAPI restAPI)
        {
            _commonData = commonData;
            _restAPI = restAPI;
        }

        public void CallAPIReload(string url, string Authorization)
        {
            try
            {
                _restAPI.SendChatBot(new RestAPIRequest()
                {
                    Body = "{}",
                    Headers = new List<RestAPIHeader>()
                     {
                         new RestAPIHeader()
                         {
                               Label = "Content-Type",
                               Value = "application/json"
                         },
                         new RestAPIHeader()
                         {
                               Label = "Authorization",
                               Value = Authorization
                         }
                     },
                    Timeout = 30,
                    Method = "GET",
                    Url = $"{url}/api/ReLoadConfigChatBotOK"
                });
            }
            catch (Exception ex)
            {
                new IboxLog($"CallAPIReload: {ex.Message} \n url: {url}", "AppLogs");
            }
        }
    }
}