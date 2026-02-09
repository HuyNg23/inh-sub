using IBox.Client.Business.Model;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.DBC.Factories;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.Database
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class DatabaseConnectionController : ActionController<DatabaseConnection, TenantContext>
    {
        private readonly IDBConnection _connection;
        private readonly IEncryption _encryption;

        public DatabaseConnectionController(IBContext<TenantContext> mainDB, IDBConnection connection, IEncryption encryption) : base(mainDB, encryption)
        {
            _connection = connection;
            _encryption = encryption;
        }

        /// <summary>
        /// Kiểm tra kết nối database
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("CheckConnect")]
        [IBoxActionPermission("integration-config-sql-manage")]
        public ResponseForm<string> CheckConnect(RequestForm<DatabaseConnection> req)
        {
            return new ResponseForm<string>(() =>
            {
                if (req.Body.ModificationDate == null)
                {
                    req.Body.Password = _encryption.Encrypt(req.Body.Password ?? "");
                }

                if (req.Body.SQLType == 3)
                {
                    return _connection.SetConnection("").CheckDbInformIxConnection(req.Body).ToString();
                }

                if (req.Body.SQLType == 4)
                {
                    return _connection.SetConnection("").CheckDbConnectionOracleSqlServer(req.Body, "ORACLE").ToString();
                }

                if (req.Body.SQLType == 0)
                {
                    return _connection.SetConnection("").CheckDbConnectionOracleSqlServer((req.Body), "SQLSERVER").ToString();

                }

                return _connection.SetConnection(req.Body).CheckConnect().ToString();
            });
        }

        [IBoxActionPermission("integration-config-sql-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var dbConnection = CheckValid(req);

                if (string.IsNullOrEmpty(dbConnection.Address?.Trim()))
                {
                    throw new IboxLog("Server name can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (dbConnection.SQLType != 3 && dbConnection.SQLType != 5)
                {
                    if (string.IsNullOrEmpty(dbConnection.Catalog?.Trim()))
                    {
                        throw new IboxLog("Category can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }

                    if (string.IsNullOrEmpty(dbConnection.Username?.Trim()))
                    {
                        throw new IboxLog("User can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }

                    if (string.IsNullOrEmpty(dbConnection.Password?.Trim()))
                    {
                        throw new IboxLog("Pass can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }
                }

                var data = base.IBContext.Context.DatabaseConnections.FirstOrDefault(ptr => ptr.Address == dbConnection.Address && ptr.Catalog == dbConnection.Catalog && ptr.Id != dbConnection.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Connect DB already exists", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                dbConnection.Password = _encryption.Encrypt(dbConnection.Password ?? "");
                return dbConnection.Update(IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-sql-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var dbConnection = CheckValid(req);

                if (string.IsNullOrEmpty(dbConnection.Address?.Trim()))
                {
                    throw new IboxLog("Server name can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (dbConnection.SQLType != 3 && dbConnection.SQLType != 5)
                {
                    if (string.IsNullOrEmpty(dbConnection.Catalog?.Trim()))
                    {
                        throw new IboxLog("Category can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }

                    if (string.IsNullOrEmpty(dbConnection.Username?.Trim()))
                    {
                        throw new IboxLog("User can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }

                    if (string.IsNullOrEmpty(dbConnection.Password?.Trim()))
                    {
                        throw new IboxLog("Pass can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                    }
                }

                var data = base.IBContext.Context.DatabaseConnections.FirstOrDefault(ptr => ptr.Address == dbConnection.Address && ptr.Catalog == dbConnection.Catalog && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Connect DB already exists", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                dbConnection.Password = _encryption.Encrypt(dbConnection.Password ?? "");
                return dbConnection.Create(IBContext);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-sql-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        [HttpPost("GetAll/{pageNumber}")]
        [IBoxActionPermission("integration-config-sql-manage", "integration-config-sql-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            try
            {
                return new ResponseForm<List<dynamic>>(() =>
                {
                    if (pageNumber == -1)
                    {
                        var queryResultPage = base.IBContext.Context.DatabaseConnections.Where(ptr => !ptr.IsDelete).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();

                        foreach (var item in queryResultPage)
                        {
                            if (item.Password != null)
                            {
                                item.Password = "*********";
                            }
                        }

                        return queryResultPage;
                    }
                    else
                    {
                        int numberOfObjectsPerPage = 30;
                        var queryResultPage = base.IBContext.Context.Context.DatabaseConnections
                            .Where(ptr => !ptr.IsDelete)
                            .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                            .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();

                        foreach (var item in queryResultPage)
                        {
                            if (item.Password != null)
                            {
                                item.Password = "*********";
                            }
                        }

                        return queryResultPage;
                    }
                });
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-sql-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [IBoxActionPermission("integration-config-sql-manage", "integration-config-sql-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }
    }

    public class ExecuteQuery
    {
        public string DatabaseID = string.Empty;
        public string Query = string.Empty;
    }
}