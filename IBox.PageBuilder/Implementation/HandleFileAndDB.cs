using IBox.Database.Root;
using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBox.Common.Security;
using IBox.Common.Objects;
using IBox.PageBuilder.Factories;
using Renci.SshNet;
using static System.Net.WebRequestMethods;
using Serilog;
using FluentFTP;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using IBox.PageBuilder.Model;

namespace IBox.PageBuilder.Implementation
{
    public class HandleFileAndDB : IHandleFileAndDB
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        public HandleFileAndDB(IConfiguration configuration, IEncryption encryption)
        {
            _configuration = configuration;
            _encryption = encryption;
        }
        public string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";

            path = path.Trim().Replace("\\", "/");
            if (!path.StartsWith("/")) path = "/" + path;
            if (!path.EndsWith("/")) path += "/";

            return path;
        }

        public bool UpdateSaveFileComponentsDB(ReqWebResource req, string tenantId)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;
            var components = tenantContext.P_Components.FirstOrDefault(ptr => ptr.Id == req.Id && !ptr.IsDelete);

            if (components == null)
            {
                return false;
            }

            EntityAction.TryUpdate<P_Component>(components, tenantContext, new P_Component
            {
                Id = req.Id,
                Name = components.Name,
                PathCss = req.CSS ?? "",
                PathHtml = req.HTML ?? "",
                PathJs = req.JavaScript ?? "",
                PathLink = req.Library ?? ""
            });
            tContext.Dispose();

            return true;
        }

        public void DeleteAssetLibraryOnDb(string id, string tenantId)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var assetLibrary = tenantContext.P_AssetLibraries.FirstOrDefault(ptr => ptr.Id == id && !ptr.IsDelete);

            if (assetLibrary != null)
            {
                EntityAction.TryDelete<P_AssetLibrary>(assetLibrary, tenantContext);
            }

            tContext.Dispose();
        }

        public void AddOrUpdateAssetLibraryDb(string name, string url, string version, string tags, string tenantId)
        {
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;

            var assetLibrary = tenantContext.P_AssetLibraries.FirstOrDefault(ptr => ptr.Url == url && !ptr.IsDelete);

            if (assetLibrary == null)
            {
                EntityAction.TryCreate<P_AssetLibrary>(null, tenantContext, new P_AssetLibrary
                {
                    Name = name,
                    Url = url,
                    Version = version,
                    Tags = tags
                });

                tenantContext.Dispose();
                return;
            }

            EntityAction.TryUpdate<P_AssetLibrary>(assetLibrary, tenantContext, new P_AssetLibrary
            {
                Id = assetLibrary.Id,
                Name = name,
                Url = url,
                Version = version,
                Tags = tags
            });

            tenantContext.Dispose();
        }
        public void DeleteFileFTP(FtpClient ftpClient, string path)
        {
            try
            {
                if (ftpClient.FileExists(path)) // Chỉ kiểm tra file, tránh nhầm với thư mục
                {
                    ftpClient.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteFile error: {ex.Message} \n Path: {path}");
            }
        }


        public void DeleteFileSFTP(SftpClient sftpClient, string path)
        {
            try
            {
                if (sftpClient.Exists(path))
                {
                    sftpClient.DeleteFile(path);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"DeleteFile: {ex.Message} \n path: {path}");
            }
        }
    }
}
