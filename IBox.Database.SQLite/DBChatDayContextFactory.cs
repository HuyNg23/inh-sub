using IBox.Database.ChatBot;
using Microsoft.EntityFrameworkCore;

namespace IBox.Database.SQLiteDBChatDay
{
    public class DBChatDayContextFactory
    {
        private readonly DbContextOptions<DBChatDayContext> _options;

        public DBChatDayContextFactory(DbContextOptions<DBChatDayContext> options)
        {
            _options = options;
        }

        public DBChatDayContext CreateContext(string dbName)
        {
            return new DBChatDayContext(_options, dbName);
        }
    }
}