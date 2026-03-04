using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.DBC.Factories;
using IBox.Database.Root;
using IBox.Database.Tenant.Tables;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Serilog;
using System.Data.Odbc;
using System.Text.Json;

namespace IBox.Database.DBC.Implementation
{
    public class DBConnection : AibContext, IDBConnection
    {
        private DatabaseConnection? connectionInfo;
        protected string baseConnectionStringPostgre = @"Host={0};Port={1};Database={2};Username={3};Password={4}";
        protected string baseConnectionStringMySQL = @"server={0};user={1};password={2};database={3}";

        //Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.1.100)(PORT = 1521)))(CONNECT_DATA = (SERVICE_NAME = ORCL))); User Id = myuser; Password=mypassword;
        protected string baseConnectionStringOracle = @"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))(CONNECT_DATA=(SERVICE_NAME={2})));User Id={3};Password={4};{5}";

        protected string baseConnectionStringTenant = @"Data Source={0};Initial Catalog={1};User ID={2};Password={3};{4}";

        public DBConnection Context => this;

        public DBConnection(IEncryption encryption,
            IConfiguration configuration) : base(configuration, encryption)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (connectionInfo != null)
            {
                ConnectSQLs(connectionInfo.SQLType, optionsBuilder);
            }
        }

        public DBConnection SetConnection(DatabaseConnection connection)
        {
            DBConnection dBConnection = new DBConnection(encryption, configuration);
            dBConnection.connectionInfo = connection;
            return dBConnection;
        }

        public DBConnection SetConnection(string connection)
        {
            DBConnection dBConnection = new DBConnection(encryption, configuration);
            return dBConnection;
        }

        public bool CheckConnect()
        {
            try
            {
                this.Database.OpenConnection();
                bool canConnect = this.Database.CanConnect();
                this.Database.CloseConnection();
                return canConnect;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        public DBConnection OpenConnect()
        {
            try
            {
                this.Database.OpenConnection();
                return this;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        public List<Dictionary<string, object>> ExecuteQuery(string sqlQuery, string tenantId)
        {
            if (string.IsNullOrEmpty(sqlQuery))
            {
                throw new IboxLog("SQL query cannot be null or empty.", tenantId);
            }

            if (this.connectionInfo == null)
            {
                throw new IboxLog("Connect infomation to DB is null", tenantId);
            }

            List<string> prohibitedUseList = new List<string>() {
                    "DROP TABLE",
                    "DROP DATABASE",
                    "DROP VIEW",
                    "DROP INDEX",
                    "TRUNCATE",
                    "IDENTIFIED",
                    "GRANT",
                    "PRIVILEGES",
                    "FLUSH",
                    "REVOKE",
                    "ALTER TABLE",
                    "BACKUP"
                };

            if (prohibitedUseList.Any(ptr => sqlQuery.ToUpper().Contains(ptr.ToUpper())))
            {
                throw new IboxLog("" +
                    "Query has contains word in prohibited use list", tenantId);
            }

            if (!this.Database.CanConnect())
            {
                throw new IboxLog(string.Format("Connect to DB {0} is failse or not exist", connectionInfo.Address), tenantId);
            }

            using (var command = this.Database.GetDbConnection().CreateCommand())
            {
                try
                {
                    command.CommandText = sqlQuery;
                    var reader = command.ExecuteReader();
                    List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                    while (reader.Read())
                    {
                        Dictionary<string, object> fields = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; ++i)
                        {
                            fields.Add(reader.GetName(i), reader[i]);
                        }
                        result.Add(fields);
                    }

                    this.Database.CloseConnection();

                    return result;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == -2)
                    {
                        command.Cancel();
                    }
                    throw;
                }
            }
        }

        public void ExecuteQueryAsync(string sqlQuery, string tenantId)
        {
            try
            {
                if (this.connectionInfo == null)
                {
                    throw new IboxLog("Connect infomation to DB is null", tenantId);
                }

                List<string> prohibitedUseList = new List<string>() {
                    "DROP TABLE",
                    "DROP DATABASE",
                    "DROP VIEW",
                    "DROP INDEX",
                    "TRUNCATE",
                    "IDENTIFIED",
                    "GRANT",
                    "PRIVILEGES",
                    "FLUSH",
                    "REVOKE",
                    "ALTER TABLE",
                    "BACKUP"
                };

                if (prohibitedUseList.Any(ptr => sqlQuery.ToUpper().Contains(ptr.ToUpper())))
                {
                    throw new IboxLog("Query has contains word in prohibited use list", tenantId);
                }

                if (!this.Database.CanConnect())
                {
                    throw new IboxLog(string.Format("Connect to DB {0} is failse or not exist", connectionInfo.Address), tenantId);
                }

                using (var command = this.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sqlQuery;
                    command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        public void ConnectSQLs(int? type, DbContextOptionsBuilder optionsBuilder)
        {
            try
            {
                var additionalOptions = string.Empty;

                if (connectionInfo != null && connectionInfo.Options != null)
                {
                    var options = JsonSerializer.Deserialize<List<OptionsConnection>>(connectionInfo.Options);

                    if (options != null)
                    {
                        additionalOptions = string.Join(";", options.Select(opt => $"{opt.key}={opt.value}"));
                    }
                }

                switch (type)
                {
                    case 1:
                        string connectStringPostgre = string.Format(baseConnectionStringPostgre,
                        connectionInfo?.Address,
                        connectionInfo?.Port,
                        connectionInfo?.Catalog,
                        connectionInfo?.Username,
                        encryption.Decrypt(connectionInfo?.Password ?? ""));
                        Log.Information($"[DB Connection - PostgreSQL] Server: {connectionInfo?.Address}, Database: {connectionInfo?.Catalog}");
                        optionsBuilder.UseNpgsql(connectStringPostgre);
                        break;

                    case 2:
                        string connectStringMySQL = string.Format(baseConnectionStringMySQL,
                        connectionInfo?.Address,
                        connectionInfo?.Username,
                        encryption.Decrypt(connectionInfo?.Password ?? ""),
                        connectionInfo?.Catalog);
                        Log.Information($"[DB Connection - MySQL] Server: {connectionInfo?.Address}, Database: {connectionInfo?.Catalog}");
                        optionsBuilder.UseMySql(connectStringMySQL, new MySqlServerVersion(new Version(8, 0, 21)));
                        break;
                    //case 3 dùng cho kết nối InformIx
                    case 4:
                        string connectStringOracle = string.Format(baseConnectionStringOracle,
                        connectionInfo?.Address,
                        connectionInfo?.Port,
                        connectionInfo?.Catalog,
                        connectionInfo?.Username,
                        encryption.Decrypt(connectionInfo?.Password ?? ""),
                        additionalOptions);
                        Log.Information($"[DB Connection - Oracle] Server: {connectionInfo?.Address}:{connectionInfo?.Port}, Database: {connectionInfo?.Catalog}");
                        optionsBuilder.UseOracle(connectStringOracle);
                        break;

                    case 5:
                        Log.Information($"[DB Connection - SQLite] Database: {connectionInfo?.Address}");
                        optionsBuilder.UseSqlite($@"Data Source={connectionInfo?.Address ?? ""};Cache=Shared;");
                        break;

                    default:
                        string connectString = string.Format(baseConnectionStringTenant,
                        connectionInfo?.Address,
                        connectionInfo?.Catalog,
                        connectionInfo?.Username,
                        encryption.Decrypt(connectionInfo?.Password ?? ""),
                        additionalOptions);
                        
                        var maskedConnectionString = string.Format(baseConnectionStringTenant,
                        connectionInfo?.Address,
                        connectionInfo?.Catalog,
                        connectionInfo?.Username,
                        "***MASKED***",
                        additionalOptions);
                        
                        Log.Information($"[DB Connection - SQL Server] Connection string: {maskedConnectionString}");
                        
                        optionsBuilder.UseSqlServer(connectString);
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"ConnectSQLs Error: {ex.Message}", "AppLogs", ex);
            }
        }

        public void CloseConnect()
        {
            try
            {
                this.Database.CloseConnectionAsync();
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        /// <summary>
        /// Kiểm tra kết nối tới database informix
        /// </summary>
        /// <param name="databaseConnection"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public bool CheckDbInformIxConnection(DatabaseConnection databaseConnection)
        {
            try
            {
                using (OdbcConnection connection = new OdbcConnection($"DSN={databaseConnection.Address}"))
                {
                    try
                    {
                        connection.Open();
                    }
                    catch (OdbcException ex)
                    {
                        throw new IboxLog("Connection database informix errors: " + ex.Message, "AppLogs");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Unexpected error: {ex.Message}", "AppLogs", ex);
            }
        }

        /// <summary>
        /// Kiểm tra kết nối tới database oracle
        /// </summary>
        /// <param name="databaseConnection"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>

        public bool CheckDbConnectionOracleSqlServer(DatabaseConnection databaseConnection, string databaseType)
        {
            try
            {
                var additionalOptions = string.Empty;

                if (databaseConnection != null && databaseConnection.Options != null)
                {
                    var options = JsonSerializer.Deserialize<List<OptionsConnection>>(databaseConnection.Options);

                    if (options != null)
                    {
                        additionalOptions = string.Join(";", options.Select(opt => $"{opt.key}={opt.value}"));
                    }
                }

                string connectionString = string.Empty;

                switch (databaseType.ToUpper())
                {
                    case "SQLSERVER":
                        connectionString = string.Format(baseConnectionStringTenant,
                            databaseConnection?.Address,
                            databaseConnection?.Catalog,
                            databaseConnection?.Username,
                            encryption.Decrypt(databaseConnection?.Password ?? ""),
                            additionalOptions);
                        break;

                    case "ORACLE":
                        connectionString = string.Format(baseConnectionStringOracle,
                            databaseConnection?.Address,
                            databaseConnection?.Port,
                            databaseConnection?.Catalog,
                            databaseConnection?.Username,
                            encryption.Decrypt(databaseConnection?.Password ?? ""),
                            additionalOptions);
                        break;

                    default:
                        throw new IboxLog($"Unsupported database type: {databaseType}", "AppLogs");
                }

                // Use the appropriate connection based on the database type
                if (databaseType.ToUpper() == "SQLSERVER")
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (SqlException ex)
                        {
                            throw new IboxLog($"Connection to SQL Server database failed: " + ex.Message, "AppLogs", ex);
                        }
                    }
                }
                else if (databaseType.ToUpper() == "ORACLE")
                {
                    using (OracleConnection connection = new OracleConnection(connectionString))
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (OracleException ex)
                        {
                            throw new IboxLog($"Connection to Oracle database failed: " + ex.Message, "AppLogs", ex);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Unexpected error: {ex.Message}", "AppLogs", ex);
            }
        }
        /// <summary>
        /// Execute query database informix async
        /// </summary>
        /// <param name="nameDSN"></param>
        /// <param name="informixQuery"></param>
        /// <exception cref="IboxLog"></exception>
        public void ExecuteQueryInformIxAsync(string nameDSN, string informixQuery, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(nameDSN))
                {
                    throw new IboxLog("An Unexpected Error Has Occurred: address is not null or empty.", tenantId);
                }

                string formatDSN = $"DSN={nameDSN}";

                List<string> prohibitedUseList = new List<string>() {
                        "DROP TABLE",
                        "DROP DATABASE",
                        "DROP VIEW",
                        "DROP INDEX",
                        "TRUNCATE",
                        "IDENTIFIED",
                        "GRANT",
                        "PRIVILEGES",
                        "FLUSH",
                        "REVOKE",
                        "ALTER TABLE",
                        "BACKUP"
                    };

                if (prohibitedUseList.Any(ptr => informixQuery.ToUpper().Contains(ptr.ToUpper())))
                {
                    throw new IboxLog("Query contains words in prohibited use list", tenantId);
                }

                using (OdbcConnection connection = new OdbcConnection(formatDSN))
                {
                    connection.Open();

                    using (OdbcCommand command = new OdbcCommand(informixQuery, connection))
                    {
                        command.CommandText = informixQuery;
                        command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }

        /// <summary>
        /// Execute query database informix
        /// </summary>
        /// <param name="nameDSN"></param>
        /// <param name="informixQuery"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public List<Dictionary<string, object>> ExecuteQueryInformIx(string nameDSN, string informixQuery, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(nameDSN))
                {
                    throw new IboxLog("An Unexpected Error Has Occurred: address is not null or empty.", tenantId);
                }

                string formatDSN = $"DSN={nameDSN}";

                List<string> prohibitedUseList = new List<string>() {
                    "DROP TABLE",
                    "DROP DATABASE",
                    "DROP VIEW",
                    "DROP INDEX",
                    "TRUNCATE",
                    "IDENTIFIED",
                    "GRANT",
                    "PRIVILEGES",
                    "FLUSH",
                    "REVOKE",
                    "ALTER TABLE",
                    "BACKUP"
                };

                if (prohibitedUseList.Exists(ptr => informixQuery.ToUpper().Contains(ptr.ToUpper())))
                {
                    throw new IboxLog("Query contains words in prohibited use list", tenantId);
                }

                using (OdbcConnection connection = new OdbcConnection(formatDSN))
                {
                    connection.Open();

                    using (OdbcCommand command = new OdbcCommand(informixQuery, connection))
                    {
                        using (OdbcDataReader reader = command.ExecuteReader())
                        {
                            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                Dictionary<string, object> fields = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; ++i)
                                {
                                    fields.Add(reader.GetName(i), reader[i]);
                                }

                                result.Add(fields);
                            }

                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantId, ex);
            }
        }
    }

    public class OptionsConnection
    {
        public string id { get; set; } = string.Empty;
        public string key { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }
}