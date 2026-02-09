using IBox.Common.Objects;
using IBox.Database.Tenant.Tables;
using IBox.PageBuilder.Factories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace IBox.PageBuilder.Implementation
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor nhận IWebHostEnvironment qua dependency injection
        public FileUploadService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Đẩy file lên thư mục wwwroot/uploadsPage/{tenantId}/{pageId}
        /// </summary>
        /// <param name="file">File được upload từ client (IFormFile)</param>
        /// <param name="tenantId">ID của tenant</param>
        /// <param name="pageId">ID của page</param>
        /// <returns>Đường dẫn tương đối của file đã upload</returns>
        public async Task<string> UploadFileAsync(IFormFile file, string tenantId, string pageId)
        {
            // Sử dụng Path.Combine để tạo subFolder tương thích với mọi hệ điều hành
            string subFolder = Path.Combine("uploadsPage", tenantId, pageId);

            // Kiểm tra file có hợp lệ không
            if (file == null || file.Length == 0)
            {
                throw new IboxLog("Không có file nào được chọn hoặc file rỗng.", tenantId);
            }

            // Đường dẫn tới thư mục wwwroot
            string webRootPath = _webHostEnvironment.WebRootPath;

            // Tạo đường dẫn đầy đủ tới thư mục lưu trữ
            string uploadFolder = Path.Combine(webRootPath, subFolder);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Tạo tên file duy nhất để tránh trùng lặp
            string uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Ghi file lên thư mục wwwroot
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối, sử dụng gạch chéo xuôi '/' cho URL
            string relativePath = $"/{subFolder.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
            return relativePath;
        }

    }
}