using DocumentFormat.OpenXml.InkML;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Data;

namespace IBox.Database.Root
{
    public class RootContext : AibContext, IBContext<RootContext>
    {
        public DbSet<T_Tenant> T_Tenants { get; set; }
        public DbSet<Sec_Package> P_Packages { get; set; }
        public DbSet<Sec_Package_Detail> P_Package_Details { get; set; }
        public DbSet<U_User> U_Users { get; set; }
        public DbSet<S_ServerScheduleStatus> S_ServerScheduleStatus { get; set; }
        public DbSet<U_User_Detail> U_User_Details { get; set; }
        public DbSet<U_Role> U_Roles { get; set; }
        public DbSet<S_Service> S_Services { get; set; }
        public DbSet<C_ConfigRoot> C_ConfigRoot { get; set; }
        public DbSet<U_User_Action_History> U_User_Action_Historys { get; set; }
        public DbSet<D_DatabaseConnection> D_DatabaseConnections { get; set; }
        public DbSet<S_ScheduleShrinkLog> S_ScheduleShrinkLogs { get; set; }
        public DbSet<S_ServerScheduleShrinkLogState> S_ServerScheduleShrinkLogStates { get; set; }
        public DbSet<A_AssetLibraryStorage> A_AssetLibraryStorages { get; set; }
        public RootContext Context => this;

        public RootContext(IConfiguration configuration, IEncryption encryption)
            : base(configuration, encryption)
        { }

        public T_Tenant GetTenant(string? tenantID)
        {
            if (tenantID == "ROOT")
            {
                throw new IboxLog("Root account can not access to tenant resources", tenantID);
            }

            if (!this.Context.Database.CanConnect())
            {
                throw new IboxLog($"Can not connect to Database of tenant: {tenantID}", tenantID);
            }

            try
            {
                var tenant = this.Context.T_Tenants.FirstOrDefault(ptr => ptr.Id == tenantID);
                this.Dispose();
                if (tenant == null)
                {
                    throw new IboxLog("Tenant was not found", "AppLogs");
                }

                return tenant;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IEnumerable<T_Tenant> GetAllTenants()
        {
            return T_Tenants.Where(ptr => !ptr.IsDelete).ToList();
        }

        public IBContext<RootContext> GetTenantContext(string? tenantID)
        {
            //if (tenantID != "ROOT")
            //{
            //    throw new IboxException("Unauthorized access is denied");
            //}

            return this;
        }

        public List<object> RawSqlQuery(string query)
        {
            var data = new List<object>();
            var context = Context;

            var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            context.Database.OpenConnection();
            using (var result = command.ExecuteReader())
            {
                while (result.Read())
                {
                    var rowData = new Dictionary<string, object>();
                    for (int i = 0; i < result.FieldCount; i++)
                    {
                        rowData[result.GetName(i)] = result[i];
                    }
                    data.Add(rowData);
                }
            }
            context.Database.CloseConnection();

            return data;
        }
        public void SyncDatabaseSchemaRoot()
        {
            try
            {
                var tables = Context.Context.Model.GetEntityTypes()
                     .Select(t => t.GetTableName())
                     .Distinct()
                     .ToList();

                foreach (var table in tables)
                {
                    if (table == null)
                    {
                        continue;
                    }

                    AddTable(Context, configuration.Config.Value.Database.Main.DatabaseName, table);
                    var entityType = Context.Context.Model.GetEntityTypes()
                        .FirstOrDefault(t => t.GetTableName() == table);

                    if (entityType != null)
                    {
                        foreach (var property in entityType.GetProperties())
                        {
                            string columnName = property.GetColumnName();
                            string columnType = property.GetColumnType();
                            AddColumn(Context, configuration.Config.Value.Database.Main.DatabaseName, table, columnName, columnType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"SyncDatabaseSchemaRoot: {ex.Message}", "AppLogs", "Info");
            }
        }
        private void AddColumn(IBContext<RootContext> rootContext, string dataBaseName, string tableName, string columnName, string columnType)
        {
            var query = $@"
                IF NOT EXISTS (SELECT * FROM {dataBaseName}.INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}')
                BEGIN
                ALTER TABLE {tableName} ADD {columnName} {columnType}
            END
            ";

            ExecuteSQLRaw(query, rootContext);
        }
        private void ExecuteSQLRaw(string query, IBContext<RootContext> tnContext)
        {
            tnContext.Context.Database.ExecuteSqlRaw(query);
        }
        private void AddTable(IBContext<RootContext> rootContext, string dataBaseName, string tableName)
        {
            var query = $@"
                IF NOT EXISTS (SELECT * FROM {dataBaseName}.INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
                BEGIN
                    CREATE TABLE {tableName} (
                        Id nvarchar(60) PRIMARY KEY
                    )
                END";

            ExecuteSQLRaw(query, rootContext);
        }
        public IBContext<RootContext> GetTenantContext()
        {
            return this;
        }
    }
}