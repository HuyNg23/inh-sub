using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Security;
using IBox.Database.Root.Tables;
using Newtonsoft.Json;
using System.Text;
using Serilog;

namespace IBox.PageBuilder.Implementation
{
    public class UpLoadAssetLibrary : IUpLoadAssetLibrary
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UpLoadAssetLibrary(IEncryption encryption, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _encryption = encryption;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }
        private bool IsValidFile(IFormFile file) => file != null && file.Length > 0;

        private void ValidateRequest(ReqUpLoadAssetLibrary req)
        {
            if (string.IsNullOrEmpty(req.Name))
            {
                throw new IboxLog("Name can't null or empty.", "AppLogs");
            }

            if (string.IsNullOrEmpty(req.Version))
            {
                throw new IboxLog("Version can't null or empty.", "AppLogs");
            }

            if (req.Files == null || !req.Files.Any(f => IsValidFile(f)))
            {
                throw new IboxLog("No valid files selected for upload.", "AppLogs");
            }
        }

        public string UpLoadFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId, string authorization)
        {
            var configAssetLibrary = CheckConfigAssetLibrary();

            ValidateRequest(req);

            if (configAssetLibrary.TypeStorage == TypeStorage.Local)
            {
                return UpLoadFileAssetLibraryLocal(req, tenantId, authorization);
            }
            else if (configAssetLibrary.TypeStorage == TypeStorage.FTP)
            {
                return "";
            }
            else if (configAssetLibrary.TypeStorage == TypeStorage.SFTP)
            {
                return "";
            }
            else
            {
                throw new IboxLog("Not found type storage.", tenantId);
            }
        }

        private string UpLoadFileAssetLibraryLocal(ReqUpLoadAssetLibrary req, string tenantId, string authorization)
        {
            var allowedExtensions = new[] { ".js", ".ts", ".css", ".scss", ".map", ".md", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".woff", ".woff2", ".ttf", ".eot", ".otf", ".mp3", ".mp4", ".webm", ".wav", ".ogg", ".flac", ".aac", ".m4a", ".flv", ".avi", ".mov", ".wmv", ".mpg", ".mpeg", ".mkv", ".webm", ".pdf", ".json", ".xml", ".yaml", ".yml", ".html" };
            var validatedFiles = new List<(IFormFile File, string RelativePath, string TargetPath, string UrlPath)>();
            var services = IBGlobalConfig.StoreService.ToList();
            var results = new List<string>();

            // Chuẩn bị danh sách file
            foreach (var file in req.Files.Where(f => f != null && f.Length > 0))
            {
                string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    results.Add($"File {file.FileName} is not allowed.");
                    continue;
                }

                string targetPath = Path.Combine("assets", tenantId, fileExtension.Replace(".", ""), req.Name, req.Version);
                string relativePath = $"/assets/{tenantId}/{fileExtension.Replace(".", "")}/{req.Name}/{req.Version}/{file.FileName}";
                string urlPath = $"/{fileExtension.Replace(".", "")}/{req.Name}/{req.Version}/{file.FileName}";

                validatedFiles.Add((file, relativePath, targetPath, urlPath));
            }

            // Xử lý từng file
            foreach (var (file, _, targetPath, urlPath) in validatedFiles)
            {
                var fileResults = new List<string>();
                var uploadedToServices = new List<(string ServiceIp, string FileName, string TargetPath)>();
                bool fileUploadSuccess = true;

                // Upload lên tất cả service
                foreach (var ip in services)
                {
                    try
                    {
                        var result = UploadFileToServiceForSingleFile(file, ip, tenantId, req.Name, req.Version, authorization);
                        fileResults.Add(result);
                        uploadedToServices.Add((ip, file.FileName, targetPath));
                    }
                    catch (Exception ex)
                    {
                        fileResults.Add($"Failed to upload {file.FileName} to {ip}: {ex.Message}");
                        fileUploadSuccess = false;
                        break;
                    }
                }

                if (fileUploadSuccess)
                {
                    // Lưu vào database ngay nếu upload thành công
                    try
                    {
                        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        AddOrUpdateAssetLibraryDb(req.Name, urlPath, req.Version, fileExtension, tenantId);
                        fileResults.Add($"Saved {file.FileName} to database successfully.");
                    }
                    catch (Exception ex)
                    {
                        fileResults.Add($"Failed to save {file.FileName} to database: {ex.Message}");
                        // Rollback file đã upload nếu lưu database thất bại
                        RollbackUploadedFile(uploadedToServices, authorization, fileResults);
                    }
                }
                else
                {
                    // Rollback file nếu upload thất bại
                    RollbackUploadedFile(uploadedToServices, authorization, fileResults);
                }

                results.AddRange(fileResults);
            }

            return $"Upload process completed: {string.Join(", ", results)}";
        }

        private string UploadFileToServiceForSingleFile(IFormFile file, string serviceIp, string tenantId, string name, string version, string authorization)
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

                    using (var content = new MultipartFormDataContent())
                    using (var fileStream = file.OpenReadStream())
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                        content.Add(fileContent, "Files", file.FileName);

                        string targetPath = $"assets/{tenantId}/{Path.GetExtension(file.FileName).Replace(".", "")}/{name}/{version}";
                        content.Add(new StringContent(targetPath), "Path");
                        content.Add(new StringContent(name), "Name");
                        content.Add(new StringContent(version), "Version");

                        var response = httpClient.PostAsync($"{serviceIp}/api/AssetLibrary/SaveFileAssetLibrary", content).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            return $"Uploaded {file.FileName} to {serviceIp} successfully";
                        }
                        else
                        {
                            var errorContent = response.Content.ReadAsStringAsync();
                            throw new Exception($"Failed to upload {file.FileName} - {response.StatusCode}: {errorContent}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload to {serviceIp}: {ex.Message}");
            }
        }

        /// <summary>
        /// Thêm mới hoặc cập nhật thư viện vào database 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="url"></param>
        /// <param name="version"></param>
        /// <param name="tags"></param>
        /// <param name="tenantId"></param>
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

        /// <summary>
        /// Lưu danh sách file từ yêu cầu vào thư mục được chỉ định dựa trên tenantId
        /// </summary>
        /// <param name="req">Thông tin yêu cầu chứa danh sách file</param>
        /// <param name="tenantId">ID của tenant</param>
        /// <returns>Chuỗi thông báo kết quả</returns>
        public string SaveFileAssetLibrary(ReqUpLoadAssetLibrary req, string tenantId)
        {
            if (req.Files == null || !req.Files.Any() || req.Files.All(f => f == null || f.Length == 0))
            {
                throw new IboxLog("No file selected or file is empty.", tenantId);
            }

            // Đường dẫn gốc wwwroot
            string webRootPath = _webHostEnvironment.WebRootPath;
            var savedFiles = new List<string>();

            try
            {
                foreach (var file in req.Files)
                {
                    if (file == null || file.Length == 0)
                    {
                        continue;
                    }

                    // Xác định đường dẫn dựa trên tenantId, loại file, name và version
                    string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    string targetFolder = fileExtension == ".js" ? "js" : "css";
                    string targetPath = Path.Combine("assets", tenantId, targetFolder, req.Name, req.Version);

                    // Tạo thư mục đầy đủ
                    string fullDirectoryPath = Path.Combine(webRootPath, targetPath);

                    // Kiểm tra và tạo thư mục nếu chưa tồn tại
                    if (!Directory.Exists(fullDirectoryPath))
                    {
                        Directory.CreateDirectory(fullDirectoryPath);
                    }

                    // Kiểm tra và làm sạch tên file
                    string fileName = Path.GetFileName(file.FileName);
                    if (string.IsNullOrEmpty(fileName) || fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                    {
                        throw new IboxLog($"Tên file không hợp lệ: {file.FileName}", tenantId);
                    }

                    // Đường dẫn file cuối cùng
                    string filePath = Path.Combine(fullDirectoryPath, fileName);

                    // Lưu file
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        file.CopyTo(fileStream);
                    }

                    // Tạo đường dẫn tương đối để trả về
                    string relativePath = "/" + Path.Combine(targetPath, fileName).Replace(Path.DirectorySeparatorChar, '/');
                    savedFiles.Add(relativePath);
                }

                return $"Lưu file thành công: {string.Join(", ", savedFiles)}";
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Lỗi khi lưu file: {ex.Message}", tenantId);
            }
        }

        /// <summary>
        /// Xóa file JS/CSS và bản ghi trong database dựa trên ID
        /// </summary>
        /// <param name="tenantId">ID của tenant</param>
        /// <param name="id">ID của bản ghi trong P_AssetLibrary</param>
        /// <returns>Chuỗi thông báo kết quả</returns>
        public bool DeleteFileAssetLibrary(string tenantId, string id, string authorization)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new IboxLog("Id asset library can't null or empty.", tenantId);
            }

            var configAssetLibrary = CheckConfigAssetLibrary();

            if (configAssetLibrary.TypeStorage == TypeStorage.Local)
            {
                return DeleteFileAssetLibraryLocal(id, authorization, tenantId);
            }
            else if (configAssetLibrary.TypeStorage == TypeStorage.FTP)
            {
                return true;
            }
            else if (configAssetLibrary.TypeStorage == TypeStorage.SFTP)
            {
                return true;
            }
            else
            {
                throw new IboxLog("Not found type storage.", tenantId);
            }
        }

        private bool DeleteFileAssetLibraryLocal(string id, string authorization, string tenantId)
        {
            var services = IBGlobalConfig.StoreService.ToList();
            var results = new List<string>();

            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;
            tContext.Dispose();
            var assetLibrary = tenantContext.P_AssetLibraries.FirstOrDefault(ptr => ptr.Id == id && !ptr.IsDelete);

            if (assetLibrary == null)
            {
                throw new IboxLog("Not found asset library.", tenantId);
            }

            foreach (var ip in services)
            {
                var result = DeleteFileFromServiceLocal(ip, assetLibrary.Url, authorization);
            }

            DeleteAssetLibraryOnDb(id, tenantId);

            return true;
        }

        private void DeleteAssetLibraryOnDb(string id, string tenantId)
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

        private bool DeleteFileFromServiceLocal(string serviceIp, string targetPath, string authorization)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

                var requestBody = new ReqUpLoadAssetLibrary
                {
                    Path = targetPath
                };

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync($"{serviceIp}/api/AssetLibrary/DeleteFileOnLocal", jsonContent).Result;

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    throw new Exception($"Failed to delete {targetPath} from {serviceIp}: {response.StatusCode} - {errorContent}");
                }

                return true;
            }
        }

        public bool DeleteFileOnLocal(string relativePath, string tenantId)
        {
            // Đường dẫn gốc wwwroot
            string webRootPath = _webHostEnvironment.WebRootPath;

            try
            {
                relativePath = $"/assets/{tenantId}/{relativePath}";
                string absolutePath = Path.Combine(webRootPath, relativePath.TrimStart('/')); // Loại bỏ '/' đầu tiên

                if (File.Exists(absolutePath))
                {
                    File.Delete(absolutePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"Lỗi khi xóa file hoặc bản ghi: {ex.Message}", tenantId);
            }
        }

        private void RollbackUploadedFile(List<(string ServiceIp, string FileName, string TargetPath)> uploadedToServices, string authorization, List<string> fileResults)
        {
            foreach (var (serviceIp, fileName, targetPath) in uploadedToServices)
            {
                try
                {
                    DeleteFileFromService(serviceIp, targetPath, fileName, authorization);
                    fileResults.Add($"Rolled back {fileName} from {serviceIp} successfully.");
                }
                catch (Exception ex)
                {
                    fileResults.Add($"Failed to rollback {fileName} from {serviceIp}: {ex.Message}");
                }
            }
        }

        private void DeleteFileFromService(string serviceIp, string targetPath, string fileName, string authorization)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            using (var httpClient = new HttpClient(handler))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
                var response = httpClient.DeleteAsync($"{serviceIp}/api/AssetLibrary/DeleteFile?path={targetPath}&fileName={fileName}").Result;

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to delete {fileName} from {serviceIp}: {response.StatusCode} - {errorContent}");
                }
            }
        }



        private A_AssetLibraryStorage CheckConfigAssetLibrary()
        {
            var rootContext = new RootContext(_configuration, _encryption);

            var configAssetLibrary = rootContext.Context.A_AssetLibraryStorages.FirstOrDefault(ptr => !ptr.IsDelete);

            rootContext.Dispose();

            if (configAssetLibrary == null)
            {
                throw new IboxLog("Asset library not configured, please contact administrator.", "AppLogs");
            }

            if (configAssetLibrary.TypeOS == null)
            {
                throw new IboxLog("Asset library not configured type OS, please contact administrator.", "AppLogs");
            }

            if (configAssetLibrary.TypeStorage == null)
            {
                throw new IboxLog("Asset library not configured type storage, please contact administrator.", "AppLogs");
            }

            return configAssetLibrary;
        }

        public bool SaveFileHTML(ReqWebResource req, string tenantId)
        {
            try
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string relativePath = Path.Combine(webRootPath, "components", tenantId, req.Id);
                string pathExtCSS = Path.Combine(relativePath, "CSS", $"{req.Name}.css").Replace("\\", "/");
                string pathExtHTML = Path.Combine(relativePath, "HTML", $"{req.Name}.html").Replace("\\", "/");
                string pathExtLibrary = Path.Combine(relativePath, "Library", $"{req.Name}.txt").Replace("\\", "/");
                string pathExtJavaScript = Path.Combine(relativePath, "JavaScript", $"{req.Name}.js").Replace("\\", "/");

                File.WriteAllText(pathExtCSS, req.CSS);
                File.WriteAllText(pathExtHTML, req.HTML);
                File.WriteAllText(pathExtLibrary, req.Library);
                File.WriteAllText(pathExtJavaScript, req.JavaScript);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"SaveFileHTML: {ex.Message}");
                throw;
            }
        }

        public async Task<ResWebResource> UrlGetFileHTML(ReqWebResource req, string tenantId)
        {
            try
            {
                var resWebResource = new ResWebResource
                {
                    Id = req.Id,
                    Name = req.Name
                };

                string webRootPath = _webHostEnvironment.WebRootPath;
                string relativePath = Path.Combine(webRootPath, "components", tenantId, req.Id);

                // Tạo đường dẫn cho các file cần đọc
                var filePaths = new Dictionary<string, string>
                {
                    { "CSS", Path.Combine(relativePath, "CSS", $"{req.Name}.css") },
                    { "HTML", Path.Combine(relativePath, "HTML", $"{req.Name}.html") },
                    { "Library", Path.Combine(relativePath, "Library", $"{req.Name}.txt") },
                    { "JavaScript", Path.Combine(relativePath, "JavaScript", $"{req.Name}.js") }
                };

                var fileContents = await Task.WhenAll(filePaths.Select(async file =>
                {
                    return new
                    {
                        FileType = file.Key,
                        Content = await ReadFileWithoutLockAsync(file.Value)
                    };
                }));

                foreach (var fileContent in fileContents)
                {
                    switch (fileContent.FileType)
                    {
                        case "CSS":
                            resWebResource.CSS = fileContent.Content;
                            break;
                        case "HTML":
                            resWebResource.HTML = fileContent.Content;
                            break;
                        case "Library":
                            resWebResource.Library = fileContent.Content;
                            break;
                        case "JavaScript":
                            resWebResource.JavaScript = fileContent.Content;
                            break;
                    }
                }

                return resWebResource;
            }
            catch (Exception ex)
            {
                Log.Error($"Lỗi khi lấy thông tin WebResource: {ex.Message}");
                throw;
            }
        }
        public async Task<string> ReadFileWithoutLockAsync(string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(fileStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"ReadFileWithoutLockAsync Lỗi khi đọc file {filePath}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

