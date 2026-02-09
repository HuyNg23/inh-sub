using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Common.Objects;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using Serilog;
using IBox.PageBuilder.Model;
using System.Net;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Implementation;

namespace IBox.Client.Controllers.XPage
{
    [Route("api/[controller]")]
    [IBoxAuthorization]
    [ApiController]
    public class ComponentController : ActionController<P_Component, TenantContext>
    {
        private readonly IFileAssetLibraryBase _fileAssetLibraryBase;
        public ComponentController(IBContext<TenantContext> tenantContext, IEncryption encryption, IFileAssetLibraryBase fileAssetLibraryBase) : base(tenantContext, encryption)
        {
            _fileAssetLibraryBase = fileAssetLibraryBase;
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-page-manage", "integration-config-page-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        /// <summary>
        /// Tạo component
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Component name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.P_Components.FirstOrDefault(ptr => ptr.Name == objModel.Name && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Component name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        /// <summary>
        /// Cập nhật component
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <exception cref="IboxLog"></exception>
        [IBoxActionPermission("integration-config-page-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var objModel = CheckValid(req);

                if (string.IsNullOrEmpty(objModel.Name?.Trim()))
                {
                    throw new IboxLog("Component name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.P_Components.FirstOrDefault(ptr => ptr.Name == objModel.Name && ptr.Id != objModel.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Component name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("SaveFileHTML")]
        public ResponseForm<dynamic> SaveFileHTML(RequestForm<ReqWebResource> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            try
            {
                var authorization = Request.Headers["Authorization"].ToString();
                return new ResponseForm<dynamic>(() =>
                {
                    return _fileAssetLibraryBase.SaveFileHTML(req.Body, tenantId, authorization);
                });
            }
            catch (Exception ex)
            {
                new IboxLog($"SaveFileHTML: {ex.Message}", tenantId, "Error");
                throw;
            }
        }
        [HttpPost("GetAllFileHtml")]
        public ResponseForm<dynamic> GetAllFileHtml(RequestForm<ReqWebResource> req)
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            try
            {
                var authorization = Request.Headers["Authorization"].ToString();
                return new ResponseForm<dynamic>(() =>
                {
                    return _fileAssetLibraryBase.GetAllFileHtml(req.Body, tenantId, authorization);
                });
            }
            catch (Exception ex)
            {
                new IboxLog($"GetAllFileHtml: {ex.Message}", tenantId, "Error");
                throw;
            }
        }
    }
}
