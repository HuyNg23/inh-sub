using IBox.Database.Root.Tables;
using IBox.PageBuilder.Model;

namespace IBox.PageBuilder.Factories
{
    public interface IHandleFileAssetLibraryFTP
    {
        byte[] GetFileFromFTP(string fileName, string tenantId, A_AssetLibraryStorage storageConfig);
        string UploadFileToFTP(ReqUpLoadAssetLibrary req, A_AssetLibraryStorage storageConfig, string tenantId);
        bool DeleteFileAssetLibraryFTP(A_AssetLibraryStorage storageConfig, string id, string authorization, string tenantId);
        bool SaveFileHTMLFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId);
        ResWebResource GetAllFileHtmlFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId);
    }
}
