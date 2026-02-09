using IBox.PageBuilder.Model;

namespace IBox.PageBuilder.Factories
{
    public interface IUpLoadAssetLibrary
    {
        string UpLoadFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId, string authorization);

        bool DeleteFileAssetLibrary(string tenantId, string id, string authorization);
        bool DeleteFileOnLocal(string tenantId, string relativePath);

        string SaveFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId);
        bool SaveFileHTML(ReqWebResource req, string tenantId);
        Task<ResWebResource> UrlGetFileHTML(ReqWebResource req, string tenantId);
    }
}
