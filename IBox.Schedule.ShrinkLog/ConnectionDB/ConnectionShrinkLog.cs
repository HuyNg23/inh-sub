using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Schedule.ShrinkLog.Model;
using Microsoft.Data.SqlClient;
using System.Text;

namespace IBox.Schedule.ShrinkLog.ConnectionDB
{
    public class ConnectionShrinkLog : IConnectionShrinkLog
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        protected string _connectionString = "Server={0};User Id={1};Password={2};Connect Timeout=30;MultiSubnetFailover=True;TrustServerCertificate=true;MultipleActiveResultSets=True;Pooling=True;Min Pool Size=5;Max Pool Size=60000;";

        public ConnectionShrinkLog(IConfiguration configuration, IEncryption encryption)
        {
            _configuration = configuration;
            _encryption = encryption;
        }

        /// <summary>
        /// Kiểm tra kết nối SQL
        /// </summary>
        /// <param name="dBConnection"></param>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public bool CheckConnectSQL(DBConnectionShrinkLog dBConnection)
        {
            try
            {
                if (string.IsNullOrEmpty(dBConnection.Address))
                {
                    throw new IboxException($"Address can't null or empty.");
                }

                if (string.IsNullOrEmpty(dBConnection.UserName))
                {
                    throw new IboxException($"UserName can't null or empty.");
                }

                if (string.IsNullOrEmpty(dBConnection.Password))
                {
                    throw new IboxException($"Password can't null or empty.");
                }

                using (SqlConnection connection = new SqlConnection(FormatConnection(dBConnection)))
                {
                    connection.Open();

                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        connection.Close();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new IboxException($"An Unexpected Error Has Occurred: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Đinh dạng chuỗi kết nối SQL
        /// </summary>
        /// <param name="dBConnection"></param>
        /// <returns></returns>
        private string FormatConnection(DBConnectionShrinkLog dBConnection)
        {
            return string.Format(_connectionString, dBConnection.Address, dBConnection.UserName, dBConnection.Password);
        }

        /// <summary>
        /// Lấy thông tin Log các DB của IBOX
        /// </summary>
        /// <returns></returns>
        /// <exception cref="IboxException"></exception>
        public List<LogSpaceInfoDatabase> GetLogSpaceInfoDatabase()
        {
            try
            {
                string listCategory = string.Empty;

                var dbRoot = this._configuration.Config.Value.Database.Main.DatabaseName ?? string.Empty;

                listCategory = $"{dbRoot},";

                var rootContext = new RootContext(this._configuration, this._encryption);

                var listTenantCode = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

                var listTenant = new Dictionary<string, string>();

                if (listTenantCode != null && listTenantCode.Count > 0)
                {
                    var sb = new StringBuilder(listCategory);

                    foreach (var tenant in listTenantCode)
                    {
                        listTenant.Add(tenant.TenantCode ?? "", tenant.TenantName ?? "");
                        var tenantCode = tenant?.TenantCode ?? "";
                        sb.Append($"{tenantCode},");
                    }

                    listCategory = sb.ToString().TrimEnd(',');
                }

                var querryCheck = "CREATE TABLE #LogSpaceInfoTemp"
                                + "(DatabaseName NVARCHAR(128),"
                                + "LogSizeMB DECIMAL(18, 2),"
                                + "LogSpaceUsedPercent DECIMAL(18, 2),"
                                + "Status INT);"
                                + "INSERT INTO #LogSpaceInfoTemp (DatabaseName, LogSizeMB, LogSpaceUsedPercent, Status)"
                                + "EXEC ('DBCC SQLPERF(LOGSPACE)');"
                                + "SELECT *"
                                + "FROM #LogSpaceInfoTemp "
                                + $"WHERE DatabaseName IN ({GetStringDBNameShrink(listCategory)});"
                                + "DROP TABLE #LogSpaceInfoTemp;";

                var result = rootContext.Context.RawSqlQuery(querryCheck);

                var logSpaceInfoList = new List<LogSpaceInfoDatabase>();

                foreach (Dictionary<string, object> record in result)
                {
                    var databaseName = record["DatabaseName"]?.ToString() ?? string.Empty;
                    var logSizeMB = record["LogSizeMB"]?.ToString() ?? string.Empty;
                    var logSpaceUsedPercent = record["LogSpaceUsedPercent"]?.ToString() ?? string.Empty;
                    var tenantName = listTenant.ContainsKey(databaseName) ? listTenant[databaseName] : databaseName;

                    var logSpaceInfo = new LogSpaceInfoDatabase
                    {
                        DatabaseName = databaseName,
                        LogSizeMB = logSizeMB,
                        LogSpaceUsedPercent = logSpaceUsedPercent,
                        TenantName = tenantName
                    };

                    logSpaceInfoList.Add(logSpaceInfo);
                }

                rootContext.Dispose();

                return logSpaceInfoList;
            }
            catch (Exception ex)
            {
                throw new IboxException($"GetLogSpaceInfoDatabase an unexpected error has occurred: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convert databaseName
        /// </summary>
        /// <param name="listCategory"></param>
        /// <returns></returns>
        private string GetStringDBNameShrink(string listCategory)
        {
            string[] parts = listCategory.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return string.Join(", ", parts.Select(p => $"'{p.Trim()}'"));
        }

        public List<Category> GetAllCategory()
        {
            var listCategory = new List<Category>();

            string dbRoot = this._configuration.Config.Value.Database.Main.DatabaseName ?? "";

            listCategory.Add(new Category()
            {
                Id = dbRoot,
                TenantCode = dbRoot,
                TenantName = dbRoot
            });

            var rootContext = new RootContext(this._configuration, this._encryption);

            var listTenantCode = rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).ToList();

            rootContext.Dispose();

            foreach (var item in listTenantCode)
            {
                listCategory.Add(new Category
                {
                    Id = item.Id,
                    TenantName = item.TenantName ?? string.Empty,
                    TenantCode = item.TenantCode ?? string.Empty
                });
            }

            return listCategory;
        }

        public class Category
        {
            public string Id { get; set; } = string.Empty;
            public string TenantName { get; set; } = string.Empty;
            public string TenantCode { get; set; } = string.Empty;
        }
    }
}
