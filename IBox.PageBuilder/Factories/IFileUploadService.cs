using Microsoft.AspNetCore.Http;

namespace IBox.PageBuilder.Factories
{
    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string tenantId, string pageId);
    }
}
