using IBox.Database.SQLiteMaster.Tables;
using IBox.Database.SQLiteMaster;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.IO;

namespace IBox.Database.SQLiteMaster
{
    public class DBChatMasterContext : DbContext, IDBChatMasterContext<DBChatMasterContext>
    {
        public DbSet<ChatStorage> ChatStorages { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DBChatMasterContext Context => this;

        public DBChatMasterContext(DbContextOptions<DBChatMasterContext> options)
             : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "DataBase");
            optionsBuilder.UseSqlite($@"Data Source={Path.Combine(path, $"chatbot{DateTime.Now.ToString("yyyyMMdd")}.db")}");
            base.OnConfiguring(optionsBuilder);
        }
    }

}
