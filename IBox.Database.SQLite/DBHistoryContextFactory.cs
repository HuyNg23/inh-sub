using IBox.Database.ChatBot;
using Microsoft.EntityFrameworkCore;

namespace IBox.Database.SQLiteDBChatDay
{
    public class DBHistoryContextFactory
    {
        private readonly DbContextOptions<DBHistoryContext> _options;

        public DBHistoryContextFactory(DbContextOptions<DBHistoryContext> options)
        {
            _options = options;
        }

        public DBHistoryContext CreateContext(string dbName)
        {
            return new DBHistoryContext(_options, dbName);
        }
    }
}