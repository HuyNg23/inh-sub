using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Permissions.Execution;
using IBox.Permissions.Model;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace IBox.Client.Controllers.XRole
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class XRoleController : ActionController<U_Role, RootContext>
    {
        private readonly IPermissionsService _permissionsTenant;
        private readonly IEncryption _encryption;
        private readonly Common.Objects.IConfiguration _configuration;

        public XRoleController(IPermissionsService permissionsTenant, IBContext<RootContext> roottContext, IEncryption encryption, Common.Objects.IConfiguration configuration) : base(roottContext, encryption)
        {
            _permissionsTenant = permissionsTenant;
            _encryption = encryption;
            _configuration = configuration;
        }

        /// <summary>
        /// Lấy danh sách quyền
        /// </summary>
        /// <param name="req"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [IBoxActionPermission("system-config-role-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<List<dynamic>>(() => _permissionsTenant.GetAllRole(tenantId, pageNumber));
        }

        /// <summary>
        /// Tạo mới quyền
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("system-config-role-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            var role = CheckValid(req);

            var rootContext = new RootContext(_configuration, _encryption).Context;

            var roleCreate = rootContext.U_Roles.FirstOrDefault(ptr => ptr.Name == role.Name && ptr.TenantId == tenantId && !ptr.IsDelete);

            if (roleCreate != null)
            {
                return new ResponseForm<dynamic>(() => throw new IboxLog("Role name already exists.", tenantId));
            }

            role.TenantId = tenantId;

            req.Body = JsonDocument.Parse(JsonConvert.SerializeObject(role)).RootElement;

            rootContext.Dispose();

            var create = base.Create(req);

            return create;
        }

        /// <summary>
        /// Cập nhật quyền
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("system-config-role-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            var role = CheckValid(req);

            var rootContext = new RootContext(_configuration, _encryption).Context;

            var roleNameExist = rootContext.U_Roles.FirstOrDefault(ptr => ptr.Id != role.Id && ptr.Name == role.Name && ptr.TenantId == tenantId && !ptr.IsDelete);

            if (roleNameExist != null)
            {
                return new ResponseForm<dynamic>(() => throw new IboxLog($"Role name already exists.", tenantId));
            }

            var roleUpdate = rootContext.U_Roles.FirstOrDefault(ptr => ptr.Id == role.Id && !ptr.IsDelete);

            if (roleUpdate == null)
            {
                return new ResponseForm<dynamic>(() => throw new IboxLog($"Not found roleId: {role.Id}", tenantId));
            }

            role.TenantId = tenantId;
            role.PermissionsKey = roleUpdate.PermissionsKey;

            req.Body = JsonDocument.Parse(JsonConvert.SerializeObject(role)).RootElement;

            rootContext.Dispose();

            var result = base.Update(req);

            return result;
        }

        /// <summary>
        /// Cấu hình quyền
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("ConfigurePermissions")]
        [IBoxActionPermission("system-config-role-manage")]
        public ResponseForm<dynamic> Config(RequestForm<ConfigPermissions> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _permissionsTenant.ConfigurePermissions(tenantId, req.Body));
        }

        [HttpPost("GetAllPermissions")]
        [IBoxActionPermission("system-config-role-view")]
        public ResponseForm<dynamic> GetAllPermissions()
        {
            return new ResponseForm<dynamic>(() => _permissionsTenant.GetAllPermissions());
        }

        /// <summary>
        /// Lấy danh sách cấu hình của quyền
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost("GetConfigurePermissions")]
        [IBoxActionPermission("system-config-role-view")]
        public ResponseForm<dynamic> GetConfigurePermissions(RequestForm<ConfigPermissions> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<dynamic>(() => _permissionsTenant.GetConfigurePermissions(tenantId, req.Body));
        }
    }
}