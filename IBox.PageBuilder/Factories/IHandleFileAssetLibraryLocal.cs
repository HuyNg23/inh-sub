using DocumentFormat.OpenXml.Office2010.Excel;
using IBox.PageBuilder.Model;
using System.Net;

namespace IBox.PageBuilder.Factories
{
    public interface IHandleFileAssetLibraryLocal
    {
        string UpLoadFileAssetLibraryLocal(ReqUpLoadAssetLibrary req, string tenantId, string authorization);
        bool DeleteFileAssetLibraryLocal(string tenantId, string id, string authorization);
        byte[] GetFileAssetLibraryLocal(string fileName, string tenantId, string authorization);
        bool SaveFileHTMLLocal(ReqWebResource req, string tenantId, string authorization);
        ResWebResource GetAllFileHtmlLocal(ReqWebResource req, string tenantId, string authorization);
    }
}
