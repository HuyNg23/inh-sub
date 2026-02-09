using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using Serilog;

namespace IBox.PageBuilder.Implementation
{
    public class FileAssetLibraryBase : IFileAssetLibraryBase
    {
        private readonly IBContext<RootContext> _rootContext;
        private readonly IHandleFileAssetLibraryLocal _handleFileAssetLibraryLocal;
        private readonly IHandleFileAssetLibraryFTP _handleFileAssetLibraryFTP;
        private readonly IHandleFileAssetLibrarySFTP _handleFileAssetLibrarySFTP;

        public FileAssetLibraryBase(IBContext<RootContext> rootContext, IHandleFileAssetLibraryLocal handleFileAssetLibraryLocal, IHandleFileAssetLibraryFTP handleFileAssetLibraryFTP, IHandleFileAssetLibrarySFTP handleFileAssetLibrarySFTP)
        {
            _rootContext = rootContext;
            _handleFileAssetLibraryLocal = handleFileAssetLibraryLocal;
            _handleFileAssetLibraryFTP = handleFileAssetLibraryFTP;
            _handleFileAssetLibrarySFTP = handleFileAssetLibrarySFTP;
        }

        public string UpLoadFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId, string authorization)
        {
            var storageConfig = _rootContext.Context.Set<A_AssetLibraryStorage>().FirstOrDefault(ptr => ptr.IsDelete == false);

            if (storageConfig == null)
            {
                return _handleFileAssetLibraryLocal.UpLoadFileAssetLibraryLocal(req, tenantId, authorization);
            }

            switch (storageConfig.TypeStorage)
            {
                case TypeStorage.FTP:
                    return _handleFileAssetLibraryFTP.UploadFileToFTP(req, storageConfig, tenantId);
                case TypeStorage.SFTP:
                    return _handleFileAssetLibrarySFTP.UploadFileToSFTP(req, storageConfig, tenantId);
                default:
                    return _handleFileAssetLibraryLocal.UpLoadFileAssetLibraryLocal(req, tenantId, authorization);
            }
        }

        public bool DeleteFileAssetLibrary(string tenantId, string id, string authorization)
        {
            var storageConfig = _rootContext.Context.Set<A_AssetLibraryStorage>().FirstOrDefault(ptr => ptr.IsDelete == false);

            if (storageConfig == null)
            {
                throw new Exception("Storage configuration not found.");
            }

            switch (storageConfig.TypeStorage)
            {
                case TypeStorage.FTP:
                    return _handleFileAssetLibraryFTP.DeleteFileAssetLibraryFTP(storageConfig, id, authorization, tenantId); ;
                case TypeStorage.SFTP:
                    return _handleFileAssetLibrarySFTP.DeleteFileAssetLibrarySFTP(storageConfig, id, authorization, tenantId);
                default:
                    return _handleFileAssetLibraryLocal.DeleteFileAssetLibraryLocal(id, authorization, tenantId);
            }
        }

        public byte[] GetFileAssetLibrary(string fileName, string tenantId, string authorization)
        {
            var storageConfig = _rootContext.Context.Set<A_AssetLibraryStorage>().FirstOrDefault(ptr => ptr.IsDelete == false);

            if (storageConfig == null)
            {
                throw new Exception("Storage configuration not found.");
            }

            switch (storageConfig.TypeStorage)
            {
                case TypeStorage.FTP:
                    return _handleFileAssetLibraryFTP.GetFileFromFTP(fileName, tenantId, storageConfig);
                case TypeStorage.SFTP:
                    return _handleFileAssetLibrarySFTP.GetFileFromSFTP(fileName, tenantId, storageConfig);
                default:
                    return _handleFileAssetLibraryLocal.GetFileAssetLibraryLocal(fileName, tenantId, authorization);
            }
        }
        public bool SaveFileHTML(ReqWebResource req, string tenantId, string authorization)
        {
            var storageConfig = _rootContext.Context.Set<A_AssetLibraryStorage>().FirstOrDefault(ptr => ptr.IsDelete == false);

            if (storageConfig == null)
            {
                return _handleFileAssetLibraryLocal.SaveFileHTMLLocal(req, tenantId, authorization);
            }

            switch (storageConfig.TypeStorage)
            {
                case TypeStorage.FTP:
                    return _handleFileAssetLibraryFTP.SaveFileHTMLFTP(req, storageConfig, tenantId);
                case TypeStorage.SFTP:
                    return _handleFileAssetLibrarySFTP.SaveFileHTMLSFTP(req, storageConfig, tenantId);
                default:
                    return _handleFileAssetLibraryLocal.SaveFileHTMLLocal(req, tenantId, authorization);
            }
        }

        public ResWebResource GetAllFileHtml(ReqWebResource req, string tenantId, string authorization)
        {
            try
            {
                //Kiểm tra trong root thực hiện lưu file theo phương thức nào
                var storageConfig = _rootContext.Context.Set<A_AssetLibraryStorage>().FirstOrDefault(ptr => ptr.IsDelete == false);

                if (storageConfig == null)
                {
                    return _handleFileAssetLibraryLocal.GetAllFileHtmlLocal(req, tenantId, authorization);
                }

                switch (storageConfig.TypeStorage)
                {
                    case TypeStorage.FTP:
                        return _handleFileAssetLibraryFTP.GetAllFileHtmlFTP(req, storageConfig, tenantId);
                    case TypeStorage.SFTP:
                        return _handleFileAssetLibrarySFTP.GetAllFileHtmlSFTP(req, storageConfig, tenantId);
                    default:
                        return _handleFileAssetLibraryLocal.GetAllFileHtmlLocal(req, tenantId, authorization);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllFileHtml: {ex.Message}");
                throw;
            }
        }
    }
}
