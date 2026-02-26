using IBox.Database.SQLiteDBChatDay;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IBox.Database.ChatBot
{
    public class DBChatDayContext : DbContext, IDBChatDayContext<DBChatDayContext>
    {
        public DbSet<ChatMessageDaily> ChatMessageDailies { get; set; }
        public DbSet<ChatSessionDaily> ChatSessionDailies { get; set; }
        public DBChatDayContext Context => this;
        private readonly string _databaseName;

        public DBChatDayContext(DbContextOptions<DBChatDayContext> options, string databaseName)
             : base(options)
        {
            _databaseName = databaseName;
            // ApplyPragmaSettings();
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword

        public void Dispose()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Xử lý dọn dẹp tài nguyên
        protected virtual void Dispose(bool disposing)
        {
            var connection = this.Database.GetDbConnection();
            if (connection != null)
            {
                if (connection.State == System.Data.ConnectionState.Connecting)
                {
                    connection.Close();
                }

                SqliteConnection.ClearPool((SqliteConnection)connection);
            }

            base.Dispose();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($@"Data Source={_databaseName};Cache=Shared;");
            base.OnConfiguring(optionsBuilder);
        }

        private void ApplyPragmaSettings()
        {
            using (var connection = this.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA synchronous = OFF;";
                    command.ExecuteNonQuery();

                    command.CommandText = "PRAGMA cache_size = 262144;";
                    command.ExecuteNonQuery();
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatSessionDaily>()
                .HasIndex(ptr => new { ptr.SessionId, ptr.IsClose })
                .HasDatabaseName("IX_SessionId_IsClose");
            modelBuilder.Entity<ChatSessionDaily>()
               .HasIndex(ptr => new { ptr.SenderId, ptr.IsClose })
               .HasDatabaseName("IX_SenderId_IsClose");
            modelBuilder.Entity<ChatSessionDaily>()
              .HasIndex(ptr => ptr.SenderId)
              .HasDatabaseName("IX_SenderId");
            modelBuilder.Entity<ChatMessageDaily>()
             .HasIndex(ptr => ptr.SessionId)
             .HasDatabaseName("IX_SessionId");
            base.OnModelCreating(modelBuilder);
        }
    }
}