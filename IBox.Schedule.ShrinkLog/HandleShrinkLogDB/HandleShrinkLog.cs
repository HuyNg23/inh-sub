using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using Microsoft.Data.SqlClient;
using Quartz;
using Serilog;

namespace IBox.Schedule.ShrinkLog.HandleShrinkLogDB
{
    public class HandleShrinkLog : IHandleShrinkLog
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IRestAPI _restAPI;
        private readonly string baseConnectionString = @"Data Source={0};User ID={1};Password={2};Connect Timeout={3};MultiSubnetFailover=True;TrustServerCertificate=true;MultipleActiveResultSets=True";

        public HandleShrinkLog(IConfiguration configuration, IEncryption encryption, IRestAPI restAPI)
        {
            _configuration = configuration;
            _encryption = encryption;
            _restAPI = restAPI;
        }

        /// <summary>
        /// Thực thi shrink log database
        /// </summary>
        /// <param name="context"></param>
        /// <exception cref="IboxException"></exception>
        public void RunShrinkLogDatabase(IJobExecutionContext context)
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var planId = dataMap.GetString("planId") ?? string.Empty;
            var listCategory = dataMap.GetString("listCategory") ?? string.Empty;
            var scheduleId = dataMap.GetString("scheduleId") ?? string.Empty;
            var jobKey = dataMap.GetString("jobKey") ?? string.Empty;
            var deploymentType = dataMap.GetString("deploymentType") ?? string.Empty;
            var methodShrinkLog = dataMap.GetString("methodShrinkLog") ?? string.Empty;

            try
            {
                Log.Information($"Start Shrink log database with listCategory:{listCategory}, jobKey: {jobKey}, scheduleId: {scheduleId}, planId: {planId}, deploymentType: {deploymentType}");

                switch (deploymentType)
                {
                    case "0":
                        RunDBStandAlone(listCategory);
                        break;
                    case "1":
                        RunDBAlwaysOn(listCategory, methodShrinkLog);
                        break;
                    default:
                        Log.Error($"Not found DeploymentType: {deploymentType} in config.");
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new IboxException($"RunShrinkLogDatabase An Unexpected Error Has Occurred: {ex.Message}", ex);
            }
        }

        private void RunDBStandAlone(string listCategory)
        {
            var listDBNameShrink = GetListDBNameShrink(listCategory);

            foreach (var dbNameShrink in listDBNameShrink)
            {
                ShrinkLogDBStandAlone(dbNameShrink);
            }

            MailAlertShrinkLog(listCategory);
        }

        private void RunDBAlwaysOn(string listCategory, string methodShrinkLog)
        {
            var listDBNameShrink = GetListDBNameShrink(listCategory);

            foreach (var dbNameShrink in listDBNameShrink)
            {
                ShrinkLogDBAlwaysOn(dbNameShrink, methodShrinkLog);
            }

            MailAlertShrinkLog(listCategory);
        }

        /// <summary>
        /// Shrink log database always on
        /// </summary>
        /// <param name="dbName"></param>
        private void ShrinkLogDBAlwaysOn(string dbName, string methodShrinkLog)
        {
            var dbConfigShrinkLog = DatabaseConnectionInfo();

            if (dbConfigShrinkLog == null)
            {
                return;
            }

            if (methodShrinkLog == MethodShrinkLog.Backup.ToString())
            {
                BackupLogDatabaseAlwaysOn(dbName);

                return;
            }

            if (methodShrinkLog == MethodShrinkLog.BackupAndShrink.ToString())
            {
                var backupAlwaysOn = BackupLogDatabaseAlwaysOn(dbName);

                if (!backupAlwaysOn)
                {
                    return;
                }

                using (SqlConnection connection = GetSqlConnection(dbConfigShrinkLog.Address ?? "", dbConfigShrinkLog.Username ?? "", dbConfigShrinkLog.Password ?? "", dbConfigShrinkLog.Timeout ?? 1200))
                {
                    try
                    {
                        connection.Open();

                        string queryCheck = $"USE {dbName}; DBCC SHRINKFILE (N'{dbName}_log' , 1, NOTRUNCATE); DBCC SHRINKFILE (N'{dbName}_log' , 1);";

                        Log.Information($"Query shrink log alwaysOn: {queryCheck}");

                        using (SqlCommand command = new SqlCommand(queryCheck, connection))
                        {
                            command.CommandTimeout = dbConfigShrinkLog.Timeout ?? 1200;
                            command.ExecuteNonQuery();
                        }

                        Log.Information($"Shrink log {dbName} operation completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"ShrinkLogDBAlwaysOn {dbName} an error occurred: {ex}");
                    }

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

            }
            else
            {
                Log.Error($"ShrinkLogDBAlwaysOn database {dbName} fail because not found methodShrinkLog: {methodShrinkLog}");
            }

        }

        /// <summary>
        /// Backup log database always on
        /// </summary>
        /// <param name="dbName"></param>
        /// <returns></returns>
        private bool BackupLogDatabaseAlwaysOn(string dbName)
        {
            try
            {
                Log.Information($"Start BackupLogDatabaseAlwaysOn {dbName}.");

                var dbConfigShrinkLog = DatabaseConnectionInfo();

                if (dbConfigShrinkLog == null)
                {
                    return false;
                }

                string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                string logFileName = $"{dbName}_Log_{currentTime}";

                using (SqlConnection connection = GetSqlConnection(dbConfigShrinkLog.Address ?? "", dbConfigShrinkLog.Username ?? "", dbConfigShrinkLog.Password ?? "", dbConfigShrinkLog.Timeout ?? 1200))
                {
                    try
                    {
                        connection.Open();

                        string queryCheck = $"USE {dbName}; BACKUP LOG [{dbName}] TO  DISK = N'{dbConfigShrinkLog.PathSaveLog}\\{logFileName}.BAK' WITH NOFORMAT, INIT, NAME = N' BAK Backup', SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10;";

                        Log.Information($"Query backup alwaysOn: {queryCheck}");

                        using (SqlCommand command = new SqlCommand(queryCheck, connection))
                        {
                            command.CommandTimeout = dbConfigShrinkLog.Timeout ?? 1200;
                            command.ExecuteNonQuery();
                        }

                        Log.Information($"BackupLogDatabaseAlwaysOn in database {dbName} operation completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"BackupLogDatabaseAlwaysOn in database {dbName} an error occurred: {ex}");
                    }

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"BackupLogDatabaseAlwaysOn in database {dbName} an error occurred: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Shrink log database stand alone
        /// </summary>
        /// <param name="database"></param>
        private void ShrinkLogDBStandAlone(string database)
        {
            var dbConfigShrinkLog = DatabaseConnectionInfo();

            if (dbConfigShrinkLog == null)
            {
                return;
            }

            using (SqlConnection connection = GetSqlConnection(dbConfigShrinkLog.Address ?? "", dbConfigShrinkLog.Username ?? "", dbConfigShrinkLog.Password ?? "", dbConfigShrinkLog.Timeout ?? 1200))
            {
                try
                {
                    connection.Open();

                    string queryCheck = $"USE {database}; "
                                       + $"ALTER DATABASE {database} SET RECOVERY SIMPLE; "
                                       + $"DBCC SHRINKFILE (N'{database}', 1); "
                                       + $"ALTER DATABASE {database} SET RECOVERY FULL; ";

                    using (SqlCommand command = new SqlCommand(queryCheck, connection))
                    {
                        command.CommandTimeout = dbConfigShrinkLog.Timeout ?? 1200;
                        command.ExecuteNonQuery();
                    }

                    Log.Information($"Shrink log DBStandAlone {database} operation completed successfully.");
                }
                catch (Exception ex)
                {
                    Log.Error($"ShrinkLogDBStandAlone {database} an error occurred: {ex.Message}");
                }

                if (connection.State == System.Data.ConnectionState.Open)
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Gửi mail sau khi shrink log
        /// </summary>
        /// <param name="ServiceName"></param>
        private void MailAlertShrinkLog(string ServiceName)
        {
            try
            {
                var thisSite = this._configuration?.Config?.Value?.ThisSite ?? string.Empty;
                Task.Run((Action)(() =>
                {
                    this._restAPI.SendMailAlert((List<string>)IBGlobalConfig.LogServiceIBox, new
                    {
                        Id = "MailAlertROOT",
                        MailTitle = $"[Warning] Shrink Log Database",
                        MailBody = $"Dear Team, <BR><BR>Shrink log database {ServiceName} Running on Server {thisSite}",
                        typeWarning = TypeWarning.ShrinkLogDb
                    });
                }));
            }
            catch (Exception ex)
            {
                Log.Error($"MailAlertShrinkLog an error has occurred: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy thông tin kết nối SQL
        /// </summary>
        /// <returns></returns>
        private D_DatabaseConnection DatabaseConnectionInfo()
        {
            var rootContext = new RootContext(this._configuration, this._encryption);

            var databaseConfigShrinkLog = rootContext.Context.D_DatabaseConnections.FirstOrDefault();

            if (databaseConfigShrinkLog == null)
            {
                Log.Error($"ConnectionInfo shrink log is null.");
                return new D_DatabaseConnection();
            }

            rootContext.Context.Dispose();

            if (!CheckValidConnection(databaseConfigShrinkLog))
            {
                Log.Error($"ConnectionInfo shrink log is invalid.");
                return new D_DatabaseConnection();
            }

            databaseConfigShrinkLog.Password = this._encryption.Decrypt(databaseConfigShrinkLog.Password ?? "");

            return databaseConfigShrinkLog;
        }

        /// <summary>
        /// Kiểm tra valid thông tin kết nối SQL
        /// </summary>
        /// <param name="databaseConnection"></param>
        /// <returns></returns>
        private bool CheckValidConnection(D_DatabaseConnection databaseConnection)
        {
            if (string.IsNullOrEmpty(databaseConnection.Address) || string.IsNullOrEmpty(databaseConnection.Username) || string.IsNullOrEmpty(databaseConnection.Password))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Formart database name shrink log
        /// </summary>
        /// <param name="listCategory"></param>
        /// <returns></returns>
        private List<string> GetListDBNameShrink(string listCategory)
        {
            var result = new List<string>();

            var elements = listCategory.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var element in elements)
            {
                result.Add(element.Trim().Replace("\"", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty));
            }

            return result;
        }

        /// <summary>
        /// Lấy kết nối SQL để shrink log
        /// </summary>
        /// <param name="server"></param>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private SqlConnection GetSqlConnection(string server, string userId, string password, int timeout)
        {
            string connectionString = string.Format(baseConnectionString, server, userId, password, timeout);
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}
