using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant.ServicesManager;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System.Data.SqlClient;
using System.Diagnostics;

namespace IBox.Schedule.Library.HandleSqlDependency
{
    public class SqlDependencyDatabase : ISqlDependencyDatabase, IDisposable
    {
        private static SqlDependencyManager? _sqlDependencyManager;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IIBGlobalConfig _iBGlobalConfig;
        private readonly IEncryption _encryption;
        private readonly IGetDataLogStream _getDataLogStream;
        private readonly string baseConenctionString = @"Data Source={0};Initial Catalog={1};User ID={2};Password={3};Connect Timeout={4};{5};Application Name={6}";
        private bool _canDispose = true;

        public SqlDependencyDatabase(IBox.Common.Objects.IConfiguration configuration, IEncryption encryption, IIBGlobalConfig iBGlobalConfig, IGetDataLogStream getDataLogStream)
        {
            _configuration = configuration;
            _encryption = encryption;
            _iBGlobalConfig = iBGlobalConfig;
            _getDataLogStream = getDataLogStream;
        }

        public void OnLoadSqlDependency(TypeUserBase typeUserBase)
        {
            try
            {
                var thisSite = IBGlobalConfig.ThisSite;
                var databaseName = _configuration?.Config?.Value?.Database?.Main.DatabaseName ?? string.Empty;
                var connectionString = typeUserBase == TypeUserBase.Tenant?  Connection("ScheduleService"): Connection("RootBEService");
                UpdateStatus(thisSite, typeUserBase);

                EnableServiceBroker(databaseName);

                if (_sqlDependencyManager == null)
                {
                    _sqlDependencyManager = new SqlDependencyManager(connectionString);
                }

                switch (typeUserBase)
                {
                    case TypeUserBase.Tenant:
                        _sqlDependencyManager.RegisterListener("S_ServerScheduleStatus",
                            "SELECT Site, Status FROM dbo.S_ServerScheduleStatus",
                            OnDatabaseS_ServerScheduleStatusChange);
                        break;

                    case TypeUserBase.Root:
                        _sqlDependencyManager.RegisterListener("S_ServerScheduleShrinkLogState",
                        "SELECT Site, Status FROM dbo.S_ServerScheduleShrinkLogState",
                        OnDatabaseS_ServerScheduleShrinkLogStateChange);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OnLoadSqlDependency in ShrinkLog an error has occurred: {ex}");
            }
        }

        #region Xử lý xự kiện

        private void OnDatabaseS_ServerScheduleStatusChange(object sender, SqlNotificationEventArgs e)
        {
            HandleDatabaseChange("S_ServerScheduleStatus", e);
        }

        private void OnDatabaseS_ServerScheduleShrinkLogStateChange(object sender, SqlNotificationEventArgs e)
        {
            HandleDatabaseChange("S_ServerScheduleShrinkLogState", e);
        }

        private void HandleDatabaseChange(string tableName, SqlNotificationEventArgs e)
        {
            try
            {
                using var rootContext = new RootContext(_configuration, _encryption);
                var thisSite = IBGlobalConfig.ThisSite;

                bool shouldUpdate = tableName switch
                {
                    "S_ServerScheduleStatus" => rootContext.Context.S_ServerScheduleStatus.Any(ptr => ptr.Site == thisSite && ptr.Status != true),
                    "S_ServerScheduleShrinkLogState" => rootContext.Context.S_ServerScheduleShrinkLogStates.Any(ptr => ptr.Site == thisSite && ptr.Status != true),
                    _ => false
                };

                if (shouldUpdate)
                {
                    string storedProc = tableName == "S_ServerScheduleStatus" ? "sp_UpdateStatusServerSchedule" : "sp_UpdateStatusServerShrinkLog";
                    rootContext.Context.Database.ExecuteSqlInterpolated($"EXEC {storedProc} 'true', {thisSite}");
                    Log.Information($"StatusServer updated for {tableName}: {thisSite} set to true.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in HandleDatabaseChange for {tableName}: {ex.Message}");
            }
        }

        #endregion

        public string Connection(string serviceName)
        {
            try
            {
                var additionalOptions = string.Empty;
                var databaseOptions = _configuration?.Config?.Value?.Database?.Options;

                if (databaseOptions != null)
                {
                    additionalOptions = string.Join(";", databaseOptions.Select(opt => $"{opt.Key}={opt.Value}"));
                }
                var connectionString = string.Format(baseConenctionString,
                                                       _configuration?.Config?.Value?.Database?.Main.ServerName,
                                                       _configuration?.Config?.Value?.Database?.Main.DatabaseName,
                                                       _configuration?.Config?.Value?.Database?.Main.UserName,
                                                       _encryption.Decrypt(_configuration?.Config?.Value?.Database?.Main?.Password ?? "SQLDefaultPassword"),
                                                       _configuration?.Config?.Value?.Database?.Main.Timeout, additionalOptions,
                                                       serviceName);

                return connectionString;
            }
            catch
            {
                throw;
            }
        }
        public void SqlDependencyStartService(string serviceName)
        {
            try
            {
                var connectionString = Connection(serviceName);
                var databaseName = _configuration?.Config?.Value?.Database?.Main.DatabaseName ?? string.Empty;

                EnableServiceBroker(databaseName);

                if (_sqlDependencyManager == null)
                {
                    _sqlDependencyManager = new SqlDependencyManager(connectionString);
                }

                _sqlDependencyManager.RegisterListener("S_Services",
                       "SELECT Site, Port FROM dbo.S_Services",
                      (sender, e) => OnDatabaseS_ServicesChange(sender, e, serviceName));
            }
            catch (Exception ex)
            {
                Log.Error($"SqlDependencyStart: {ex.Message}");
            }
        }
        public void OnDatabaseS_ServicesChange(object sender, SqlNotificationEventArgs e, string serviceName)
        {
            _iBGlobalConfig.OnLoad();
            if (serviceName == "LogService")
            {
                //Kiểm tra global có thay đổi giữa schedule và restservice
                var newRestData = IBGlobalConfig.RestServiceIBox
                    .Where(ptr => ptr.Contains(IBGlobalConfig.ThisSite))
                    .ToList();

                var newScheduleData = IBGlobalConfig.ScheduleServiceIBox
                    .Where(ptr => ptr.Contains(IBGlobalConfig.ThisSite))
                    .ToList();

                bool isChangedRest = !new HashSet<string>(newRestData).SetEquals(IBGlobalConfig.RestServiceIBoxGetDataLog);
                bool isChangedSchedule = !new HashSet<string>(newScheduleData).SetEquals(IBGlobalConfig.ScheduleServiceIBoxGetDataLog);

                if (isChangedRest || isChangedSchedule)
                {
                    IBGlobalConfig.RestServiceIBoxGetDataLog = newRestData;
                    IBGlobalConfig.ScheduleServiceIBoxGetDataLog = newScheduleData;
                    _getDataLogStream.GetDataLog();
                }
            }
        }

        public void EnableServiceBroker(string databaseName)
        {
            try
            {
                var rootContext = new RootContext(_configuration, _encryption);
                string queryCheckEnabledBroker = $"SELECT is_broker_enabled FROM sys.databases WHERE name = '{databaseName}';";
                var checkEnabledBroker = rootContext.Context.RawSqlQuery(queryCheckEnabledBroker);
                var dataCheckEnabledBroker = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(JsonConvert.SerializeObject(checkEnabledBroker));

                if (dataCheckEnabledBroker != null && checkEnabledBroker.Any())
                {
                    foreach (var row in dataCheckEnabledBroker)
                    {
                        foreach (var pair in row)
                        {
                            var key = pair.Key;

                            var value = pair.Value.ToString() ?? string.Empty;

                            if (key == "is_broker_enabled" && value.ToUpper() != "TRUE")
                            {
                                rootContext.Context.RawSqlQuery($"alter database [{databaseName}] set enable_broker with rollback immediate;");
                            }
                        }
                    }

                    rootContext.Context.Dispose();
                }
                else
                {
                    rootContext.Context.RawSqlQuery($"alter database [{databaseName}] set enable_broker with rollback immediate;");

                    rootContext.Context.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"EnableServiceBroker an error has occurred: {ex}");
            }
        }

        private void UpdateStatus(string thisSite, TypeUserBase typeUserBase)
        {
            try
            {
                var rootContext = new RootContext(_configuration, _encryption);

                if (typeUserBase == TypeUserBase.Tenant)
                {
                    rootContext.Context.Database.ExecuteSqlInterpolated($"EXEC sp_UpdateStatusServerSchedule 'true', {thisSite}");
                }

                if (typeUserBase == TypeUserBase.Root)
                {
                    rootContext.Context.Database.ExecuteSqlInterpolated($"EXEC sp_UpdateStatusServerShrinkLog 'true', {thisSite}");
                }

                rootContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"UpdateStatus an error has occurred: {ex.Message}", ex);
            }
        }
        public void DisableDispose()
        {
            _canDispose = false;
        }

        public void Dispose()
        {
            try
            {
                if (!_canDispose) return;
                var rootContext = new RootContext(_configuration, _encryption);
                var service = rootContext.Context.S_Services.FirstOrDefault(ptr => ptr.Site == IBGlobalConfig.ThisSite && ptr.Port == _configuration.Config.Value.Port && ptr.SubDomain == _configuration.Config.Value.SubDomain);
                
                Log.Information($"Dispose: {JsonConvert.SerializeObject(service)}");
                if (service != null)
                {
                    rootContext.Context.S_Services.Remove(service);
                    rootContext.Context.SaveChanges();
                }

                rootContext.Context.Dispose();
                _sqlDependencyManager?.Dispose();
                _sqlDependencyManager = null;
            }
            catch (Exception ex)
            {
                Log.Error($"Dispose SQLDependency: {ex.Message}");
            }
        }

        public void CleanupConversations(string queryRemoveEndpoints)
        {
            try
            {
                var connectionString = Connection("ScheduleService");
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(queryRemoveEndpoints, conn))
                    {
                        cmd.CommandTimeout = 0;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in CleanupConversations: {ex}");
            }
        }
    }
}