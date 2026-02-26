using IBox.Database.SQLiteDBChatDay.TableDBHistory;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IBox.Database.ChatBot
{
    public class DBHistoryContext : DbContext, IDBHistoryContext<DBHistoryContext>
    {
        public DbSet<H_ApiThirdPartyExecuteHistory> H_ApiThirdPartyExecuteHistories { get; set; }
        public DbSet<H_WorkflowExecuteHistory> H_WorkflowExecuteHistories { get; set; }
        public DbSet<H_SQLExecuteHistory> H_SQLExecuteHistories { get; set; }
        public DBHistoryContext Context => this;
        private readonly string _databaseName;

        public DBHistoryContext(DbContextOptions<DBHistoryContext> options, string databaseName)
             : base(options)
        {
            _databaseName = databaseName;
            // ApplyPragmaSettings();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($@"Data Source={_databaseName};Cache=Shared;");
            base.OnConfiguring(optionsBuilder);
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
            modelBuilder.Entity<H_WorkflowExecuteHistory>()
              .HasIndex(ptr => ptr.CreatedDate)
              .HasDatabaseName("IX_H_WorkflowExecuteHistory_CreatedDate");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
             .HasIndex(ptr => ptr.Status)
             .HasDatabaseName("IX_H_WorkflowExecuteHistory_Status");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
             .HasIndex(ptr => ptr.KeyExecuteRunWorkFlow)
             .HasDatabaseName("IX_H_WorkflowExecuteHistory_KeyExecuteRunWorkFlow");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
              .HasIndex(ptr => ptr.RequestWF)
              .HasDatabaseName("IX_H_WorkflowExecuteHistory_Request");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
              .HasIndex(ptr => ptr.ResultWF)
              .HasDatabaseName("IX_H_WorkflowExecuteHistory_Response");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
              .HasIndex(ptr => ptr.WfName)
              .HasDatabaseName("IX_H_WorkflowExecuteHistory_WorkflowName");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
               .HasIndex(ptr => ptr.SiteRun)
               .HasDatabaseName("IX_H_WorkflowExecuteHistory_SiteRun");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
                .HasIndex(ptr => new { ptr.CreatedDate, ptr.Status })
                .HasDatabaseName("IX_H_WorkflowExecuteHistory_CreatedDate_Status");

            modelBuilder.Entity<H_WorkflowExecuteHistory>()
            .HasIndex(ptr => new { ptr.CreatedDate, ptr.WfName, ptr.WfId, ptr.Status, ptr.RequestWF, ptr.ResultWF, ptr.SiteRun })
            .HasDatabaseName("IX_H_WorkflowExecuteHistory_Wfid_CreatedDate_Status_RequestWF_SiteRun_ResultWF_WfName");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
            .HasIndex(ptr => ptr.CreatedDate)
            .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_CreatedDate");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
               .HasIndex(ptr => ptr.KeyExecuteRunApiThirdParty)
               .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_KeyExecuteRunApiThirdParty");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
              .HasIndex(ptr => ptr.Status)
              .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_Status");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
          .HasIndex(ptr => ptr.SiteRun)
          .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_SiteRun");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
               .HasIndex(ptr => ptr.Request)
               .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_Request");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
               .HasIndex(ptr => ptr.Response)
               .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_Response");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
               .HasIndex(ptr => new { ptr.CreatedDate, ptr.Status })
               .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_CreatedDate_Status");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
               .HasIndex(ptr => new { ptr.CreatedDate, ptr.Status, ptr.SiteRun })
               .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_CreatedDate_Status_SiteRun");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>()
              .HasIndex(ptr => new { ptr.CreatedDate, ptr.WfId, ptr.WfName, ptr.StatusCode, ptr.Status, ptr.Request, ptr.Response, ptr.Url, ptr.StepId, ptr.SiteRun, ptr.StepName })
              .HasDatabaseName("IX_H_ApiThirdPartyExecuteHistory_Wfid_CreatedDate_StatusCode_Status_StepId_SiteRun_Url_StepName");

            modelBuilder.Entity<H_SQLExecuteHistory>()
               .HasIndex(ptr => ptr.CreatedDate)
               .HasDatabaseName("IX_H_SQLExecuteHistory_CreatedDate");

            modelBuilder.Entity<H_SQLExecuteHistory>()
               .HasIndex(ptr => ptr.KeyExecuteRunQuerySQL)
               .HasDatabaseName("IX_H_SQLExecuteHistory_KeyExecuteRunQuerySQL");

            modelBuilder.Entity<H_SQLExecuteHistory>()
              .HasIndex(ptr => ptr.Status)
              .HasDatabaseName("IX_H_SQLExecuteHistory_Status");

            modelBuilder.Entity<H_SQLExecuteHistory>()
          .HasIndex(ptr => ptr.SiteRun)
          .HasDatabaseName("IX_H_SQLExecuteHistory_SiteRun");

            modelBuilder.Entity<H_SQLExecuteHistory>()
               .HasIndex(ptr => new { ptr.CreatedDate, ptr.Status })
               .HasDatabaseName("IX_H_SQLExecuteHistory_CreatedDate_Status");

            modelBuilder.Entity<H_SQLExecuteHistory>()
               .HasIndex(ptr => new { ptr.CreatedDate, ptr.Status, ptr.SiteRun })
               .HasDatabaseName("IX_H_SQLExecuteHistory_CreatedDate_Status_SiteRun");

            modelBuilder.Entity<H_SQLExecuteHistory>()
              .HasIndex(ptr => new { ptr.CreatedDate, ptr.WfId, ptr.WfName, ptr.Status, ptr.StepId, ptr.SiteRun, ptr.StepName, ptr.Address, ptr.Catalog })
              .HasDatabaseName("IX_H_SQLExecuteHistory_Wfid_CreatedDate_Status_StepId_SiteRun_Url_StepName");

            modelBuilder.Entity<H_ApiThirdPartyExecuteHistory>(entity =>
            {
                entity.HasKey(e => e.Id); // Khóa chính
                entity.Property(e => e.KeyExecuteRunApiThirdParty)
                    .HasMaxLength(60)
                    .IsRequired()
                    .IsUnicode(false); // Tùy chọn nếu muốn lưu ASCII
                entity.HasIndex(e => e.KeyExecuteRunApiThirdParty)
                    .IsUnique(); // Đảm bảo UNIQUE
            });

            modelBuilder.Entity<H_WorkflowExecuteHistory>(entity =>
            {
                entity.HasKey(e => e.Id); // Khóa chính
                entity.Property(e => e.KeyExecuteRunWorkFlow)
                    .HasMaxLength(60)
                    .IsRequired()
                    .IsUnicode(false); // Tùy chọn nếu muốn lưu ASCII
                entity.HasIndex(e => e.KeyExecuteRunWorkFlow)
                    .IsUnique(); // Đảm bảo UNIQUE
            });

            modelBuilder.Entity<H_SQLExecuteHistory>(entity =>
            {
                entity.HasKey(e => e.Id); // Khóa chính
                entity.Property(e => e.KeyExecuteRunQuerySQL)
                    .HasMaxLength(60)
                    .IsRequired()
                    .IsUnicode(false); // Tùy chọn nếu muốn lưu ASCII
                entity.HasIndex(e => e.KeyExecuteRunQuerySQL)
                    .IsUnique(); // Đảm bảo UNIQUE
            });
        }
    }
}