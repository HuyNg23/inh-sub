using static IBox.ChatBot.DB.Models.CreateFile.ChatBotMessage;

namespace IBox.ChatBot.Service
{
    public interface IFileProcessing
    {
        public void SendAPILogInsertDBIC(RootChatBot rootChatBot, string bodyData, string tenantid);
    }
}