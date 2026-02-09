using IBox.Database.Root.Tables;
using IBox.PageBuilder.Model;

namespace IBox.PageBuilder.Factories
{
    public interface IFileAssetLibraryBase
    {
        string UpLoadFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId, string authorization);
        bool DeleteFileAssetLibrary(string tenantId, string id, string authorization);
        byte[] GetFileAssetLibrary(string fileName, string tenantId, string authorization);
        bool SaveFileHTML(ReqWebResource req, string tenantId, string authorization);
        ResWebResource GetAllFileHtml(ReqWebResource req, string tenantId, string authorization);
    }
}
