using IBox.Database.Root;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using Microsoft.AspNetCore.Http;
using System.Net.Security;
using System.Net;
using IBox.Database.Tenant.Tables;
using IBox.Database.Tenant;
using IBox.Common.Security;
using IBox.Common.Objects;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Text;
using Serilog;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.TCP;
using IBox.Common.ServiceIB;

namespace IBox.PageBuilder.Implementation
{
    public class HandleFileAssetLibraryLocal : IHandleFileAssetLibraryLocal
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHandleFileAndDB _handleFileAndDB;
        private readonly IRestAPI _restAPI;

        public HandleFileAssetLibraryLocal(IEncryption encryption, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, IHandleFileAndDB handleFileAndDB, IRestAPI restAPI)
        {
            _encryption = encryption;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _handleFileAndDB = handleFileAndDB;
            _restAPI = restAPI;
        }

        public string UpLoadFileAssetLibraryLocal(ReqUpLoadAssetLibrary req, string tenantId, string authorization)
        {
            var allowedExtensions = new[] { ".js", ".ts", ".css", ".scss", ".map", ".md", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".webp", ".woff", ".woff2", ".ttf", ".eot", ".otf", ".mp3", ".mp4", ".webm", ".wav", ".ogg", ".flac", ".aac", ".m4a", ".flv", ".avi", ".mov", ".wmv", ".mpg", ".mpeg", ".mkv", ".webm", ".pdf", ".json", ".xml", ".yaml", ".yml", ".html" };
            var validatedFiles = new List<(IFormFile File, string RelativePath, string TargetPath, string UrlPath)>();
            var services = IBGlobalConfig.StoreService.ToList();
            var results = new List<string>();

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

            foreach (var (file, _, targetPath, urlPath) in validatedFiles)
            {
                var fileResults = new List<string>();
                var uploadedToServices = new List<(string ServiceIp, string FileName, string TargetPath)>();
                bool fileUploadSuccess = true;

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
                    try
                    {
                        string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                        _handleFileAndDB.AddOrUpdateAssetLibraryDb(req.Name, urlPath, req.Version, fileExtension, tenantId);
                        fileResults.Add($"Saved {file.FileName} to database successfully.");
                    }
                    catch (Exception ex)
                    {
                        fileResults.Add($"Failed to save {file.FileName} to database: {ex.Message}");
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
        public bool DeleteFileAssetLibraryLocal(string id, string authorization, string tenantId)
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

            _handleFileAndDB.DeleteAssetLibraryOnDb(id, tenantId);

            return true;
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
        public byte[] GetFileAssetLibraryLocal(string fileName, string tenantId, string authorization)
        {
            string webRootPath = _webHostEnvironment.WebRootPath;

            string relativePath = $"/assets/{tenantId}/{fileName}";
            string filePath = Path.Combine(webRootPath, relativePath.TrimStart('/'));

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.");
            }

            return File.ReadAllBytes(filePath);
        }

        public bool SaveFileHTMLLocal(ReqWebResource req, string tenantId, string authorization)
        {
            try
            {
                bool checkSaveFile = true;
                var services = IBGlobalConfig.StoreService.ToList();
                foreach (var ip in services)
                {
                    try
                    {
                        string urlStoreService = string.Format(@"{0}{1}",
                            ip,
                            UrlServiceIBConfig.UrlSaveFileHTML
                          );

                        var result = _restAPI.Send(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(req),
                            Headers = new List<RestAPIHeader>
                            {
                                  new RestAPIHeader()
                                  {
                                        Label = "Content-Type",
                                        Value = "application/json"
                                  }
                            },
                            Method = "POST",
                            Timeout = 30,
                            Url = urlStoreService
                        }, tenantId);

                    }
                    catch (Exception ex)
                    {
                        checkSaveFile = false;
                        Log.Error($"SaveFileHTMLLocal: {ex.Message}");
                    }
                }

                if (checkSaveFile)
                {
                    req.CSS = Path.Combine("CSS", $"{req.Name}.css");
                    req.HTML = Path.Combine("HTML", $"{req.Name}.html");
                    req.Library = Path.Combine("Library", $"{req.Name}.txt");
                    req.JavaScript = Path.Combine("JavaScript", $"{req.Name}.js");
                    return _handleFileAndDB.UpdateSaveFileComponentsDB(req, tenantId);
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                Log.Error($"SaveFileHTMLLocal: {ex.Message}");
                throw;
            }
        }
        public ResWebResource GetAllFileHtmlLocal(ReqWebResource req, string tenantId, string authorization)
        {
            try
            {
                var services = IBGlobalConfig.StoreService.ToList();
                foreach (var ip in services)
                {
                    try
                    {
                        string urlStoreService = string.Format(@"{0}{1}",
                            ip,
                            UrlServiceIBConfig.UrlGetFileHTML
                          );

                        var result = _restAPI.Send(new RestAPIRequest()
                        {
                            Body = JsonConvert.SerializeObject(req),
                            Headers = new List<RestAPIHeader>
                            {
                                  new RestAPIHeader()
                                  {
                                        Label = "Content-Type",
                                        Value = "application/json"
                                  }
                            },
                            Method = "POST",
                            Timeout = 30,
                            Url = urlStoreService
                        }, tenantId);

                        if (result != null && result.Result != null)
                        {
                            return JsonConvert.DeserializeObject<ResWebResource>(result.Result) ?? new ResWebResource();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"GetAllFileHtmlLocal: {ex.Message}");
                    }
                }

                return new ResWebResource();
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllFileHtmlLocal: {ex.Message}");
                throw;
            }
        }
    }
}
