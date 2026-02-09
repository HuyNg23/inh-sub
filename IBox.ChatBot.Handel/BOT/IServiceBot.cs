using WebAPI.Model.IC;

namespace IBox.ChatBot.Service.BOT
{
    public interface IServiceBot
    {
        public Ress Send_Message(string tenantid, SendMessage sms);

        public ReponeUploadfile Upload_File(string tenantid, Uploadfile sms);

        public Ress Disable_Bot(string tenantid, GetListHistory sms);

        public Ress Enable_Bot(string tenantid, GetListHistory sms);

        public void EndChatIC(string tenantid, Ress ress, GetListHistory obj);
    }
}