using IBox.ChatBot.History.Model;
using IBox.Common.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.ChatBot.History.History
{
    public interface IHandelChatSessionDaily
    {
        public ResponseGetChatSessionDaily GetChatSessionDaily(RequestGetChatSessionDaily requestGetChatSessionDaily, string Authorize);

        public ResponseGetChatSessionDaily GetDataChatSession(RequestGetChatSessionDaily requestGetChatSessionDaily);

        public ResponseGetChatMessageDaily GetChatMessageDaily(RequestGetChatMessageDaily requestGetChatMessageDaily, string Authorize);

        public ResponseGetChatMessageDaily GetDataChatMessage(RequestGetChatMessageDaily requestGetChatMessageDaily);

        public ResponseHistoryZipFile ShowHistoryZipFile(RequestHistoryZipFile requestHistoryZipFile);

        public ResponseSizeFolder ShowSizeDriveFolder(TypeUserBase typeUserBase);

        List<ResponseSizeFolder> GetAllSizeDriveFolderChatBot(HttpRequest requestContext);

        ResponseSizeFolder ShowSizeDriveFolderOnRing(string tenantId);

        public ResponseGetSenderDaily GetSenderDaily(RequestGetSender requestGetSender, string? Authorize);

        public ResponseGetSenderDaily GetSender(RequestGetSender requestGetSender);
    }
}