using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Schedule.Library.Model;
using IBox.Schedule.Library.ShrinkLogConnectionDB;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class DatabaseConnectionController : ActionController<D_DatabaseConnection, RootContext>
    {
        private readonly IBContext<RootContext> _rootContext;
        private readonly IEncryption _encryption;
        private readonly IConnectionShrinkLog _connectionShrinkLog;

        public DatabaseConnectionController(IBContext<RootContext> context, IBContext<RootContext> rootContext, IEncryption encryption, IConnectionShrinkLog connectionShrinkLog) : base(context)
        {
            _rootContext = rootContext;
            _encryption = encryption;
            _connectionShrinkLog = connectionShrinkLog;
        }

        /// <summary>
		/// Kiểm tra kết nối database
		/// </summary>
		/// <param name="req"></param>
		/// <returns></returns>
		[HttpPost("CheckConnect")]
        public ResponseForm<dynamic> CheckConnect(RequestForm<DBConnectionShrinkLog> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _connectionShrinkLog.CheckConnectSQL(req.Body);
            });
        }

        /// <summary>
        /// Cập nhật kết nối sql trên root
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var dbConnection = CheckValid(req);

                if (string.IsNullOrEmpty(dbConnection.Address?.Trim()))
                {
                    throw new IboxLog("Server name can't null or Empty", "AppLogs");
                }

                if (string.IsNullOrEmpty(dbConnection.Username?.Trim()))
                {
                    throw new IboxLog("User can't null or Empty", "AppLogs");
                }

                if (string.IsNullOrEmpty(dbConnection.Password?.Trim()))
                {
                    throw new IboxLog("Pass can't null or Empty", "AppLogs");
                }

                var data = _rootContext.Context.D_DatabaseConnections.FirstOrDefault(ptr =>
                    ptr.Address == dbConnection.Address
                    && ptr.Id != dbConnection.Id
                    && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Connect DB already exists", "AppLogs");
                }

                var dbConnect = _rootContext.Context.D_DatabaseConnections.FirstOrDefault(ptr => ptr.Id == dbConnection.Id && !ptr.IsDelete);

                dbConnection.Password = _encryption.Encrypt(dbConnection.Password ?? "");

                dbConnect.TryUpdate(_rootContext.Context, new D_DatabaseConnection()
                {
                    Id = dbConnection.Id,
                    Address = dbConnection.Address,
                    Password = dbConnection.Password,
                    DeploymentType = dbConnection.DeploymentType,
                    Username = dbConnection.Username,
                    PathSaveLog = dbConnection.PathSaveLog,
                    Timeout = dbConnection.Timeout
                });

                return new ResponseForm<dynamic>(() => dbConnection);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Tạo mới kết nối trên root
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var dbConnection = CheckValid(req);

                if (string.IsNullOrEmpty(dbConnection.Address?.Trim()))
                {
                    throw new IboxLog("Server name can't null or Empty", "AppLogs");
                }

                if (string.IsNullOrEmpty(dbConnection.Username?.Trim()))
                {
                    throw new IboxLog("User can't null or Empty", "AppLogs");
                }

                if (string.IsNullOrEmpty(dbConnection.Password?.Trim()))
                {
                    throw new IboxLog("Pass can't null or Empty", "AppLogs");
                }

                var data = _rootContext.Context.D_DatabaseConnections.FirstOrDefault(ptr => ptr.Address == dbConnection.Address && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Connect DB already exists", "AppLogs");
                }

                dbConnection.Password = _encryption.Encrypt(dbConnection.Password ?? "");

                return new ResponseForm<dynamic>(() => EntityAction.TryCreate<D_DatabaseConnection>(null, _rootContext.Context, new D_DatabaseConnection()
                {
                    Address = dbConnection.Address,
                    Password = dbConnection.Password,
                    DeploymentType = dbConnection.DeploymentType,
                    Username = dbConnection.Username,
                    PathSaveLog = dbConnection.PathSaveLog,
                    Timeout = dbConnection.Timeout
                }));
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetAllCategory")]
        public ResponseForm<dynamic> GetAllCategory()
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _connectionShrinkLog.GetAllCategory();
            });
        }

        /// <summary>
        /// Lấy thông tin log toàn bộ database IBox
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetLogSpaceInfoDatabase")]
        public ResponseForm<dynamic> GetLogSpaceInfoDatabase()
        {
            return new ResponseForm<dynamic>(() =>
            {
                return _connectionShrinkLog.GetLogSpaceInfoDatabase();
            });
        }
    }
}