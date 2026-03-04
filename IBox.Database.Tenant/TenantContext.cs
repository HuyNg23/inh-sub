using DocumentFormat.OpenXml.InkML;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant.Tables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace IBox.Database.Tenant
{
    public class TenantContext : AibContext, IBContext<TenantContext>
    {
        public DbSet<Obj> Objs { get; set; }
        public DbSet<Obj_Schema> Obj_Schemas { get; set; }
        public DbSet<DBStructure> DBStructures { get; set; }
        public DbSet<WF_Define> WF_Defines { get; set; }
        public DbSet<WF_Step> WF_Steps { get; set; }
        public DbSet<WF_Step_Edge> WF_Step_Edges { get; set; }
        public DbSet<DynamicLinkedLibrary> DynamicLinkedLibraries { get; set; }
        public DbSet<ThirdPartyIntegrationConfiguration> ThirdPartyIntegrationConfigurations { get; set; }
        public DbSet<W_EventSocket> W_EventSockets { get; set; }
        public DbSet<W_WebSocket> W_WebSockets { get; set; }
        public DbSet<RestRequestForGadgetTool> RestRequestForGadgetTools { get; set; }
        public DbSet<DatabaseConnection> DatabaseConnections { get; set; }
        public DbSet<MailServerConnection> MailServerConnections { get; set; }
        public DbSet<Metadata> Metadataes { get; set; }
        public DbSet<S_Schedule> S_Schedules { get; set; }
        public DbSet<S_ImplementationHistory> S_ImplementationHistorys { get; set; }
        public DbSet<GroupDynamicLinkedLibrary> GroupDynamicLinkedLibraries { get; set; }
        public DbSet<CB_ConfigChat> CB_ConfigChats { get; set; }
        public DbSet<CB_ZipFileHistory> CB_ZipFileHistories { get; set; }
        public DbSet<CB_Customer> CB_Customers { get; set; }
        public DbSet<SQLite_HistoryDay> SQLite_HistoryDays { get; set; }
        public DbSet<SQLite_HistoryMonth> SQLite_HistoryMonths { get; set; }
        public DbSet<P_Component> P_Components { get; set; }
        public DbSet<P_ComponentInLayout> P_ComponentInLayouts { get; set; }
        public DbSet<P_Layout> P_Layouts { get; set; }
        public DbSet<P_AssetLibrary> P_AssetLibraries { get; set; }

        public TenantContext Context
        => this;

        private readonly IBContext<RootContext> RContext;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public TenantContext(IConfiguration configuration, IEncryption encryption, IBContext<RootContext> rContext)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(configuration, encryption)
        {
            this.RContext = rContext;
        }

        public T_Tenant TenantInfo { get; set; }

        /// <summary>
        /// Thực thi lấy object context từ root để tra cứu và khởi tạo context cho tenant, sau khi thời tạo context cho tenant thành công thì thực hiện disconnect root context
        /// </summary>
        /// <param name="tenantID"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public IBContext<TenantContext> GetTenantContext(string? tenantID)
        {
            try
            {
                TenantContext tenantContext = new TenantContext(this.configuration, this.encryption, this.RContext);
                tenantContext.TenantInfo = this.RContext.Context.GetTenant(tenantID);
                return tenantContext;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", tenantID ?? "AppLogs", ex);
            }
        }

        /// <summary>
        /// Thực thi lấy object context từ root để tra cứu và khởi tạo context cho tenant, sau khi thời tạo context cho tenant thành công thì thực hiện disconnect root context
        /// </summary>
        /// <param name="tenantID"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public IBContext<TenantContext> GetTenantContext()
        {
            try
            {
                TenantContext tenantContext = new TenantContext(this.configuration, this.encryption, this.RContext);
                return tenantContext;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", "AppLogs", ex);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                return;
            }

            modelBuilder.Entity<SQLite_HistoryDay>()
              .HasIndex(h => new { h.Type, h.Date })
              .HasDatabaseName("IX_SQLite_HistoryDay_Type_Date");

            modelBuilder.Entity<SQLite_HistoryDay>()
              .HasIndex(h => new { h.Type, h.Date, h.Site })
              .HasDatabaseName("IX_SQLite_HistoryDay_Type_Date_Site");

            modelBuilder.Entity<SQLite_HistoryDay>()
                .HasIndex(h => h.Hour)
                .HasDatabaseName("IX_SQLite_HistoryDay_Hour");

            modelBuilder.Entity<SQLite_HistoryMonth>()
              .HasIndex(h => new { h.Type, h.DateReal, h.Site })
              .HasDatabaseName("IX_SQLite_HistoryDay_Type_DateReal_Site");

            modelBuilder.Entity<SQLite_HistoryMonth>()
             .HasIndex(h => new { h.Type, h.DateReal })
             .HasDatabaseName("IX_SQLite_HistoryDay_Type_DateReal");

            modelBuilder.Entity<SQLite_HistoryMonth>()
              .HasIndex(h => new { h.Type, h.Date })
              .HasDatabaseName("IX_SQLite_HistoryDay_Type_Date");

            modelBuilder.Entity<WF_Step>()
                .HasIndex(h => h.ParentStep)
                .HasDatabaseName("IX_WF_Steps_ParentStep");

            modelBuilder.Entity<WF_Step_Edge>()
                .HasIndex(h => h.WFid)
                .HasDatabaseName("IX_WF_Step_Edges_WFid");

            modelBuilder.Entity<WF_Define>()
                .HasIndex(h => h.CreatedDate)
                .HasDatabaseName("IX_WF_Defines_CreatedDate");

            modelBuilder.Entity<S_ImplementationHistory>()
                .HasIndex(h => h.CreatedDate)
                .HasDatabaseName("IX_S_ImplementationHistorys_CreatedDate");

            modelBuilder.Entity<S_ImplementationHistory>()
                .HasIndex(h => h.Site)
                .HasDatabaseName("IX_S_ImplementationHistorys_Site");

            modelBuilder.Entity<S_ImplementationHistory>()
                .HasIndex(h => new { h.CreatedDate, h.StatusPlan })
                .HasDatabaseName("IX_S_ImplementationHistorys_CreatedDate_StatusPlan");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => c.CreatedDate)
                .HasDatabaseName("IX_CB_Customers_CreatedDate");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => c.Phone)
                .HasDatabaseName("IX_CB_Customers_Phone");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => new { c.CreatedDate, c.Phone })
                .HasDatabaseName("IX_CB_Customers_CreatedDate_Phone");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => new { c.CreatedDate, c.Phone, c.ContactId })
                .HasDatabaseName("IX_CB_Customers_CreatedDate_Phone_ContactId");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => c.ContactId)
                .HasDatabaseName("IX_CB_Customers_ContactId");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => new { c.CreatedDate, c.ContactId })
                .HasDatabaseName("IX_CB_Customers_CreatedDate_ContactId");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(c => new { c.CreatedDate, c.SenderId })
                .HasDatabaseName("IX_CB_Customer_CreatedDate_SenderId");

            modelBuilder.Entity<CB_Customer>()
                .HasIndex(e => e.SenderId)
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public void EnsureViewsCreated()
        {
            try
            {
                using (var connection = this.Database.GetDbConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        //                   command.CommandText = @"
                        //           DECLARE @sql1 NVARCHAR(MAX);
                        //           DECLARE @index NVARCHAR(MAX);
                        //           IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'VW_CB_Customers')
                        //                           BEGIN
                        //                          set @sql1 = 'CREATE VIEW VW_CB_Customers
                        //		   WITH SCHEMABINDING
                        //		   AS
                        //                               SELECT
                        //                                   SenderId,
                        //                                   CustomerInfo,
                        //                                   cif_list_data,
                        //                                   Phone,
                        //                                   ContactId,
                        //                                   Id,
                        //                                   CreatedDate,
                        //                                   IsDelete,
                        //                                   ModificationDate
                        //                               FROM dbo.CB_Customers;'
                        //                exec sp_executesql @sql1;
                        //                           END;

                        //           IF OBJECT_ID('dbo.VW_CB_Customers', 'U') IS NOT NULL
                        //               BEGIN
                        //                   DROP TABLE dbo.VW_CB_Customers;
                        //               END;

                        //           IF NOT EXISTS (
                        //               SELECT 1
                        //               FROM sys.indexes
                        //               WHERE name = 'IX_VW_CB_Customers_ContactId'
                        //                     AND object_id = OBJECT_ID('VW_CB_Customers')
                        //           )
                        //           BEGIN
                        //             set @index =  'CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_ContactId
                        //               ON VW_CB_Customers (ContactId);'
                        // exec sp_executesql @index;
                        //           END;

                        //           IF NOT EXISTS (
                        //                               SELECT 1
                        //                               FROM sys.indexes
                        //                               WHERE name = 'IX_VW_CB_Customers_SenderId'
                        //                                     AND object_id = OBJECT_ID('VW_CB_Customers')
                        //                           )
                        //           BEGIN
                        //set @index = 'CREATE NONCLUSTERED INDEX  IX_VW_CB_Customers_SenderId
                        //               ON VW_CB_Customers (SenderId);'
                        // exec sp_executesql @index;
                        //           END;

                        //            IF NOT EXISTS (
                        //                               SELECT 1
                        //                               FROM sys.indexes
                        //                               WHERE name = 'IX_VW_CB_Customers_Phone'
                        //                                     AND object_id = OBJECT_ID('VW_CB_Customers')
                        //                           )
                        //           BEGIN
                        //set @index = 'CREATE NONCLUSTERED INDEX  IX_VW_CB_Customers_Phone
                        //               ON VW_CB_Customers (Phone);'
                        // exec sp_executesql @index;
                        //           END;

                        //  IF NOT EXISTS (
                        //                               SELECT 1
                        //                               FROM sys.indexes
                        //                               WHERE name = 'IX_VW_CB_Customers_CreatedDate_SenderId'
                        //                                     AND object_id = OBJECT_ID('VW_CB_Customers')
                        //                           )
                        //           BEGIN
                        //set @index = 'CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_CreatedDate_SenderId
                        //               ON VW_CB_Customers (CreatedDate, SenderId);'
                        // exec sp_executesql @index;
                        //           END;

                        //  IF NOT EXISTS (
                        //                               SELECT 1
                        //                               FROM sys.indexes
                        //                               WHERE name = 'IX_VW_CB_Customers_CreatedDate_ContactId'
                        //                                     AND object_id = OBJECT_ID('VW_CB_Customers')
                        //                           )
                        //           BEGIN
                        //set @index = 'CREATE NONCLUSTERED INDEX IX_VW_CB_Customers_CreatedDate_ContactId
                        //               ON VW_CB_Customers (CreatedDate, ContactId);'
                        // exec sp_executesql @index;
                        //           END;
                        //       ";
                        //                   command.ExecuteNonQuery();

                        command.CommandText = @"
                            CREATE PROCEDURE InsertOrUpdateCustomers
                            @sender_id nvarchar(265) = '',
                            @customerInfo nvarchar(265) = '',
                            @cif_list_data nvarchar(265) = '',
                            @phoneNo nvarchar(265) = '',
                            @contactId nvarchar(265) = ''

                            AS
                            BEGIN
	                             IF EXISTS (SELECT 1 FROM CB_Customers WHERE SenderId = @sender_id)
                                    UPDATE CB_Customers
                                    SET CustomerInfo = @customerInfo, cif_list_data = @cif_list_data, Phone = @phoneNo, ContactId = @contactId
                                    WHERE SenderId = @sender_id;
                                ELSE
                                   INSERT INTO [dbo].[CB_Customers]
                                       ([SenderId]
                                       ,[CustomerInfo]
                                       ,[cif_list_data]
                                       ,[Phone]
                                       ,[ContactId]
                                       ,[CreatedDate]
                                       ,[ModificationDate]
                                       ,[IsDelete]
                                       ,[Id])
                                 VALUES
                                       (@sender_id
                                       ,@customerInfo
                                       ,@cif_list_data
                                       ,@phoneNo
                                       ,@contactId
                                       ,getdate()
                                       ,getdate()
                                       ,0
                                       ,newid())
                            END
                            ";
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"EnsureViewsCreated: Create View và Drop table view tạo thừa {ex.Message}");
            }
        }

        public void SyncDatabaseSchema()
        {
            try
            {
                var databases = RContext.Context.T_Tenants.Where(ptr => ptr.IsDelete == false).ToList();

                foreach (var dataBaseName in databases)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(dataBaseName.TenantCode))
                        {
                            continue;
                        }
                        var context = new TenantContext(this.configuration, this.encryption, new RootContext(this.configuration, this.encryption));
                        var tenantContext = context.GetTenantContext(dataBaseName.Id).Context;
                        var tables = tenantContext.Context.Model.GetEntityTypes()
                             .Select(t => t.GetTableName())
                             .Distinct()
                             .ToList();

                        foreach (var table in tables)
                        {
                            if (table == null)
                            {
                                continue;
                            }

                            AddTable(tenantContext, dataBaseName.TenantCode, table);
                            var entityType = tenantContext.Context.Model.GetEntityTypes()
                                .FirstOrDefault(t => t.GetTableName() == table);

                            if (entityType != null)
                            {
                                foreach (var property in entityType.GetProperties())
                                {
                                    string columnName = property.GetColumnName();
                                    string columnType = property.GetColumnType();
                                    AddColumn(tenantContext, dataBaseName.TenantCode, table, columnName, columnType);
                                }
                            }
                        }

                        tenantContext.Context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"SyncDatabaseSchema: {ex.Message} \n tenantId: {dataBaseName.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SyncDatabaseSchema: {ex.Message}");
            }
        }


        private void AddColumn(IBContext<TenantContext> tnContext, string dataBaseName, string tableName, string columnName, string columnType)
        {
            var query = $@"
                IF NOT EXISTS (SELECT * FROM {dataBaseName}.INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}')
                BEGIN
                ALTER TABLE {tableName} ADD {columnName} {columnType}
            END
            ";

            ExecuteSQLRaw(query, tnContext);
        }
        private void ExecuteSQLRaw(string query, IBContext<TenantContext> tnContext)
        {
            tnContext.Context.Database.ExecuteSqlRaw(query);
        }
        private void AddTable(IBContext<TenantContext> tnContext, string dataBaseName, string tableName)
        {
            var query = $@"
                IF NOT EXISTS (SELECT * FROM {dataBaseName}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
                BEGIN
                    CREATE TABLE {tableName} (
                        Id nvarchar(60) PRIMARY KEY
                    )
                END";

            ExecuteSQLRaw(query, tnContext);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (this.TenantInfo == null)
            {
                throw new IboxLog("Can not connect to Root Database", "AppLogs");
            }

            var additionalOptions = string.Empty;

            if (this.configuration?.Config?.Value?.Database?.Options != null)
            {

                additionalOptions = string.Join(";", this.configuration?.Config?.Value?.Database?.Options?.Select(opt => $"{opt.Key}={opt.Value}"));

            }

            var serverName = this.configuration?.Config?.Value?.Database?.Main.ServerName;
            var databaseName = this.TenantInfo.TenantCode;
            var userName = this.configuration?.Config?.Value?.Database?.Main.UserName;
            var timeout = this.configuration?.Config?.Value?.Database?.Main.Timeout;
            
            var connectionString = string.Format(this.baseConenctionString,
                                                serverName,
                                                databaseName,
                                                userName,
                                                this.encryption.Decrypt(this.configuration?.Config?.Value?.Database?.Main?.Password ?? "SQLDefaultPassword"),
                                                timeout, 
                                                additionalOptions);

            // Log connection string (with password masked)
            var maskedConnectionString = string.Format(this.baseConenctionString,
                                                serverName,
                                                databaseName,
                                                userName,
                                                "***MASKED***",
                                                timeout,
                                                additionalOptions);
            
            Log.Information($"[DB Connection - Tenant] Attempting to connect to tenant database with connection string: {maskedConnectionString}");
            Log.Information($"[DB Connection - Tenant] TenantId: {this.TenantInfo.Id}, ServerName: {serverName}, DatabaseName: {databaseName}, UserName: {userName}, Timeout: {timeout}");

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}