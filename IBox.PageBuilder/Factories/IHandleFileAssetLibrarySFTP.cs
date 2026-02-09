using IBox.Database.Root.Tables;
using IBox.PageBuilder.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.PageBuilder.Factories
{
    public interface IHandleFileAssetLibrarySFTP
    {
        byte[] GetFileFromSFTP(string fileName, string tenantId, A_AssetLibraryStorage storageConfig);

        string UploadFileToSFTP(ReqUpLoadAssetLibrary req, A_AssetLibraryStorage storageConfig, string tenantId);

        bool DeleteFileAssetLibrarySFTP(A_AssetLibraryStorage storageConfig, string id, string authorization, string tenantId);
        bool SaveFileHTMLSFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId);
        ResWebResource GetAllFileHtmlSFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId);
    }
}
