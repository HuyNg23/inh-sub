using System.Data.Common;

namespace IBox.ChatBot.HandleScheduleSQL
{
    public interface ICreateDB
    {
        public void CreateDBLiteChatBot(string tenantId);

        public void CreateDBSQLiteHistory(string tenantId);

        public string GetFilePath(string Parth, DateTime messageDate);

        public void ExecuteSQLite(DbCommand command, string querry);
        public string CreateDBSQLiteHistorySizeTime(string pathNow, DateTime dateTime);
        public string CreateDBSQLiteHistorySizeSTT(string pathNow);
    }
}