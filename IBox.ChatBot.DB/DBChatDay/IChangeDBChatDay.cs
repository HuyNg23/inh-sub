using IBox.ChatBot.DB.Models.ChatBot;
using IBox.ChatBot.DB.Models.CreateFile.IBox;
using IBox.ChatBot.DB.Models.CRM;
using IBox.ChatBot.DB.Models.FPT;
using IBox.Database.SQLiteDBChatDay;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;

namespace IBox.ChatBot.DB.DBChatDay
{
    public interface IChangeDBChatDay
    {
        public void ExecuteMerge(
           string tenantId,
           string tableName,
           string keyColumn,
           List<string> columns,
           List<List<object>> values,
           int retryCount = 3,
           int delayMilliseconds = 1000);
        public (List<string> Columns, List<List<object>> Values) ExtractColumnsAndValues<T>(List<T> entities, params string[] excludeProperties);
        public string ChangeDBChatDayCustomer(CB_ConfigChat configChat, string? senderId, int checkCount = 0);

        public string? CheckSessionDB(CB_ConfigChat configChat, string tenantId, string sessionId, ChatSessionDaily CheckSession, int checkCount = 0);

        public void ChangeDBChatDayChatMessageDaily(CB_ConfigChat configChat, ResponseChatDaySession responseChatDaySession, RequestInsertDataChatBot requestInserDataChatBot, bool isAgentChat = false, int checkCount = 0);

        public void RequestUserSupport(RequestInsertDataChatBot requestInserDataChatBot, ResponseChatDaySession chatDaySession);

        public void CheckSessionRequestSupport(Models.CreateFile.ChatBotMessage.RootChatBot chatBot, string tenantId, ResponseChatDaySession chatDaySession);

        public void EndChatIC(CB_ConfigChat configChat, string tenantId, string senderId);

        public Models.CreateFile.IBox.ListDataIBox ShowDataChatBot(string? senderId, string? tenantId, string? sessionId, string? dateTimeCurrent);

        public CB_Customer GetDataCustomer(string tenantId, string? senderId, string? idBot);

        public CB_Customer GetCustomer(string tenantId, string senderId, string? idBot);

        public Models.CreateFile.IBox.ListDataIBox GetDataChatBot(string senderId,string tenantId, string sessionId, string dateTimeCurrent);

        public void EndChatAll();

        public void ZipFileChatBot();

        public void RemoveFileZip();

        public void ChatBotSendIC(RequestShowData requestShowData);

        public List<ResponseChatBotGetSenderId> ShowAllDataChatBot(RequestShowData requestShowData);

        public List<ResponseChatBotGetSenderId> GetDataChatBotSenderId(string? tenantId, string? senderId);

        public void SaveDataChat(CB_ConfigChat configChat);

        public void CreateSessionIC(string tenantId, string user_social_id, string customerId, string idic);

        public void IdentificationCustomer(CB_ConfigChat configChat);

        public SessionObj CheckAndCreateSession(string tenantId, string senderId);

        public void RemoveSessionId(string senderId);
        public List<string> ConvertPathDB(string path, DateTime dateTime, CB_ConfigChat configChat);

        public void UpdateInteractionChat(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId);

        public void UpdateInteractionChatSub(RequestUpdateInteractionChatBot requestUpdateInteractionChatBot, string tenantId);

        public void UpdateContactChat(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId);

        public void UpdateContactChatSub(RequestUpdateContactChatBot requestUpdateContactChatBot, string tenantId);

        public void InputICIsSupport(RequestInsertDataChatBot? requestInsertDataChatBot);

        public ChatSessionDaily? GetChatSessionDaily(string tenantId, string senderId);

        public void InsertOrUpdateCustomerChat(InfoCustomer infoCustomer, string tenantId);
    }
}