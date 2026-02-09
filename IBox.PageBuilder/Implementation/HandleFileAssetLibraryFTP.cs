using DocumentFormat.OpenXml;
using FluentFTP;
using FluentFTP.Exceptions;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.PageBuilder.Factories;
using IBox.PageBuilder.Model;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using Serilog;
using System.Net;
using System.Text;

namespace IBox.PageBuilder.Implementation
{
    public class HandleFileAssetLibraryFTP : IHandleFileAssetLibraryFTP
    {
        private readonly ICommonData _commonData;
        private readonly IHandleFileAndDB _handleFileAndDB;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        public HandleFileAssetLibraryFTP(ICommonData commonData, IHandleFileAndDB handleFileAndDB, IConfiguration configuration, IEncryption encryption)
        {
            _commonData = commonData;
            _handleFileAndDB = handleFileAndDB;
            _configuration = configuration;
            _encryption = encryption;
        }

        public byte[] GetFileFromFTP(string fileName, string tenantId, A_AssetLibraryStorage storageConfig)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentNullException(nameof(fileName));
                if (storageConfig == null)
                    throw new ArgumentNullException(nameof(storageConfig));

                using var ftpClient = new FtpClient(storageConfig.Host, storageConfig.UserName, storageConfig.Password)
                {
                    Port = int.Parse(storageConfig.Port)
                };

                ftpClient.Connect();

                string filePath = Path.Combine(storageConfig.PathStorage, "assets", tenantId, fileName).Replace("\\", "/");

                bool fileExists = ftpClient.FileExists(filePath);
                if (!fileExists)
                {
                    throw new FileNotFoundException($"File '{fileName}' not found at path: {filePath}");
                }

                using var memoryStream = new MemoryStream();
                bool success = ftpClient.DownloadStream(memoryStream, filePath);

                if (!success)
                {
                    throw new IOException("Failed to download file from FTP server");
                }

                return memoryStream.ToArray();
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (FtpAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("FTP authentication failed", ex);
            }
            catch (FtpCommandException ex)
            {
                throw new IOException($"FTP command failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error downloading file from FTP: {ex.Message}", ex);
            }
        }

        public string UploadFileToFTP(ReqUpLoadAssetLibrary req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            if (req?.Files == null || req.Files.Count == 0)
                throw new ArgumentNullException(nameof(req.Files), "File list cannot be null or empty");

            if (storageConfig == null)
                throw new ArgumentNullException(nameof(storageConfig), "Storage configuration cannot be null");

            try
            {
                using var ftpClient = new FtpClient(storageConfig.Host, storageConfig.UserName, storageConfig.Password)
                {
                    Port = int.Parse(storageConfig.Port)
                };

                ftpClient.ValidateCertificate += (control, e) => e.Accept = true;
                ftpClient.Connect();

                string basePath = _handleFileAndDB.NormalizePath(storageConfig.PathStorage);
                var allowedExtensions = new HashSet<string>
                    {
                        ".js", ".ts", ".css", ".scss", ".map", ".md", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
                        ".webp", ".woff", ".woff2", ".ttf", ".eot", ".otf", ".mp3", ".mp4", ".webm", ".wav", ".ogg", ".flac",
                        ".aac", ".m4a", ".flv", ".avi", ".mov", ".wmv", ".mpg", ".mpeg", ".mkv", ".webm", ".pdf", ".json", ".xml", ".yaml", ".yml", ".html"
                    };

                var uploadedFiles = new List<string>();
                var uploadedPathFiles = new List<string>();
                bool allFilesUploaded = true;
                string lastUploadedFile = "";

                foreach (var file in req.Files)
                {
                    if (file?.Length == 0) continue;
                    if (file == null)
                    {
                        continue;
                    }

                    string fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        uploadedFiles.Add($"File {file.FileName} is not allowed.");
                        continue;
                    }

                    try
                    {
                        string urlPath = Path.Combine(fileExtension.TrimStart('.'), req.Name, req.Version, file.FileName);
                        string fullPath = Path.Combine(basePath, "assets", tenantId, urlPath);
                        fullPath = _commonData.CreateFolderFTP(ftpClient, fullPath);
                        uploadedPathFiles.Add(fullPath);
                        lastUploadedFile = fullPath;

                        string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");

                        try
                        {
                            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                            {
                                file.CopyTo(fileStream);
                            }

                            FtpStatus status = ftpClient.UploadFile(tempFilePath, fullPath, FtpRemoteExists.Overwrite, true);
                            if (status != FtpStatus.Success || !ftpClient.FileExists(fullPath))
                                throw new IOException($"File '{file.FileName}' was not uploaded successfully to {fullPath}");

                            uploadedFiles.Add(fullPath);
                        }
                        finally
                        {
                            if (File.Exists(tempFilePath))
                            {
                                File.Delete(tempFilePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        uploadedFiles.Add($"Failed to upload {file.FileName} to {storageConfig.Host}: {ex.Message}");
                        allFilesUploaded = false;
                        break;
                    }
                }

                if (allFilesUploaded)
                {
                    try
                    {
                        _handleFileAndDB.AddOrUpdateAssetLibraryDb(req.Name, lastUploadedFile, req.Version, Path.GetExtension(lastUploadedFile), tenantId);
                        uploadedFiles.Add($"Saved {lastUploadedFile} to database successfully.");
                    }
                    catch (Exception ex)
                    {
                        uploadedFiles.Add($"Failed to save {lastUploadedFile} to database: {ex.Message}");
                        RollbackUploadedFiles(ftpClient, uploadedPathFiles);
                    }
                }
                else
                {
                    RollbackUploadedFiles(ftpClient, uploadedPathFiles);
                }

                return $"Successfully uploaded {uploadedFiles.Count} files to FTP: {string.Join(", ", uploadedFiles)}";
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (FtpAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("FTP authentication failed", ex);
            }
            catch (FtpCommandException ex)
            {
                throw new IOException($"FTP upload failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading files to FTP: {ex.Message}", ex);
            }
        }

        public bool SaveFileHTMLFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            try
            {
                using var ftpClient = new FtpClient(storageConfig.Host, storageConfig.UserName, storageConfig.Password)
                {
                    Port = int.Parse(storageConfig.Port)
                };

                ftpClient.ValidateCertificate += (control, e) => e.Accept = true;
                ftpClient.Connect();

                string basePath = _handleFileAndDB.NormalizePath(storageConfig.PathStorage);
                string relativePath = Path.Combine(basePath, "components", tenantId, req.Id).Replace("\\", "/");
                List<(string Path, string Content)> files = new()
                {
                    (Path.Combine(relativePath, "CSS", $"{req.Name}.css"), req.CSS??""),
                    (Path.Combine(relativePath, "HTML", $"{req.Name}.html"), req.HTML ?? ""),
                    (Path.Combine(relativePath, "Library", $"{req.Name}.txt"), req.Library ?? ""),
                    (Path.Combine(relativePath, "JavaScript", $"{req.Name}.js"), req.JavaScript ?? "")
                };

                foreach (var (path, content) in files)
                {
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    string ftpFilePath = _commonData.CreateFolderFTP(ftpClient, path);

                    var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.tmp");
                    try
                    {
                        File.WriteAllText(tempFilePath, content);
                        ftpClient.UploadFile(tempFilePath, ftpFilePath, FtpRemoteExists.Overwrite);
                    }
                    finally
                    {
                        File.Delete(tempFilePath);
                    }
                }

                ftpClient.Disconnect();

                req.CSS = Path.Combine("CSS", $"{req.Name}.css");
                req.HTML = Path.Combine("HTML", $"{req.Name}.html");
                req.Library = Path.Combine("Library", $"{req.Name}.txt");
                req.JavaScript = Path.Combine("JavaScript", $"{req.Name}.js");
                _handleFileAndDB.UpdateSaveFileComponentsDB(req, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"SaveFileHTMLLocal: {ex.Message} \n request: {JsonConvert.SerializeObject(req)}");
            }

            return false;
        }
        /// <summary>
        /// Rollback các file đã upload nếu xảy ra lỗi
        /// </summary>
        private void RollbackUploadedFiles(FtpClient ftpClient, List<string> uploadedPathFiles)
        {
            foreach (var path in uploadedPathFiles)
            {
                _handleFileAndDB.DeleteFileFTP(ftpClient, path);
            }
        }
        public bool DeleteFileAssetLibraryFTP(A_AssetLibraryStorage storageConfig, string id, string authorization, string tenantId)
        {
            var results = new List<string>();
            var tContext = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
            var tenantContext = tContext.GetTenantContext(tenantId).Context;
            tContext.Dispose();
            var assetLibrary = tenantContext.P_AssetLibraries.FirstOrDefault(ptr => ptr.Id == id && !ptr.IsDelete);

            if (assetLibrary == null || storageConfig == null)
            {
                throw new IboxLog("Not found asset library.", tenantId);
            }

            DeleteFileFromFTP(assetLibrary.Url, storageConfig, tenantId);

            _handleFileAndDB.DeleteAssetLibraryOnDb(id, tenantId);

            return true;
        }
        public void DeleteFileFromFTP(string pathName, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(pathName))
                    throw new ArgumentNullException(nameof(pathName));
                if (storageConfig == null)
                    throw new ArgumentNullException(nameof(storageConfig));

                using var ftpClient = new FtpClient(
                    host: storageConfig.Host,
                    user: storageConfig.UserName,
                    pass: storageConfig.Password
                )
                {
                    Port = int.Parse(storageConfig.Port) // Thường là 21 cho FTP
                };

                ftpClient.Connect();

                string filePath = Path.Combine(storageConfig.PathStorage, "assets", tenantId, pathName).Replace("\\", "/");

                if (!ftpClient.FileExists(filePath))
                {
                    throw new FileNotFoundException($"File '{pathName}' not found at path: {filePath}");
                }

                ftpClient.DeleteFile(filePath);

                if (ftpClient.FileExists(filePath))
                {
                    throw new IOException($"Failed to delete file '{pathName}' at {filePath}");
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (FtpAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("FTP authentication failed", ex);
            }
            catch (FtpCommandException ex)
            {
                throw new IOException($"FTP delete operation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting file from FTP: {ex.Message}", ex);
            }
        }
        public ResWebResource GetAllFileHtmlFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            string basePath = _handleFileAndDB.NormalizePath(storageConfig.PathStorage);
            string relativePath = Path.Combine(basePath, "components", tenantId, req.Id);
            string localBasePath = Path.Combine(Directory.GetCurrentDirectory(), "components", tenantId, req.Id);
            string localBasePathTenant = Path.Combine(Directory.GetCurrentDirectory(), "components", tenantId).Replace("\\", "/");

            FtpClient ftpClient = null;
            bool ftpAvailable = true;

            try
            {
                ftpClient = new FtpClient(storageConfig.Host, storageConfig.UserName, storageConfig.Password)
                {
                    Port = int.Parse(storageConfig.Port)
                };

                ftpClient.ValidateCertificate += (control, e) => e.Accept = true;
                ftpClient.Connect();
            }
            catch (Exception ex)
            {
                ftpAvailable = false;
                Log.Error($"Không thể kết nối FTP: {ex.Message} → sẽ đọc từ local.");
            }

            var result = new ResWebResource
            {
                Id = req.Id,
                Name = req.Name,
                CSS = ReadFileWithFallback(ftpClient, ftpAvailable, relativePath, localBasePath, "CSS", req.Name, ".css", req.CheckNew),
                HTML = ReadFileWithFallback(ftpClient, ftpAvailable, relativePath, localBasePath, "HTML", req.Name, ".html", req.CheckNew),
                Library = ReadFileWithFallback(ftpClient, ftpAvailable, relativePath, localBasePath, "Library", req.Name, ".txt", req.CheckNew),
                JavaScript = ReadFileWithFallback(ftpClient, ftpAvailable, relativePath, localBasePath, "JavaScript", req.Name, ".js", req.CheckNew)
            };

            if (ftpAvailable)
            {
                ftpClient.Disconnect();
            }

            CleanupOldFilesAndFolders(localBasePathTenant, 10);
            return result;
        }

        private string ReadFileWithFallback(FtpClient? ftpClient, bool ftpAvailable, string remoteRoot, string localRoot, string subFolder, string fileName, string extension, bool CheckNew)
        {
            string remotePath = Path.Combine(remoteRoot, subFolder, fileName + extension).Replace("\\", "/");
            string localPath = Path.Combine(localRoot, subFolder, fileName + extension).Replace("\\", "/");

            try
            {
                if (!ftpAvailable || ftpClient == null)
                {
                    if (File.Exists(localPath))
                    {
                        using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (var reader = new StreamReader(fileStream))
                            {
                                string fileContent = reader.ReadToEnd();
                                return fileContent;
                            }
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }
                }

                _commonData.CreateFolder(localPath);

                if (File.Exists(localPath) && CheckNew == false)
                {
                    return File.ReadAllText(localPath, Encoding.UTF8);
                }

                var fileInfo = ftpClient.GetObjectInfo(remotePath);
                if (fileInfo == null) return string.Empty;

                var status = ftpClient.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, FtpVerify.None);
                if (status == FtpStatus.Success)
                {
                    return File.ReadAllText(localPath, Encoding.UTF8);
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error($"Lỗi đọc file {remotePath}: {ex.Message}");
                if (File.Exists(localPath))
                {
                    using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new StreamReader(fileStream))
                        {
                            string fileContent = reader.ReadToEnd();
                            return fileContent;
                        }
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public void CleanupOldFilesAndFolders(string rootPath, int inactiveMinutes = 10)
        {
            if (!Directory.Exists(rootPath)) return;

            var now = DateTime.UtcNow;
            var files = Directory.GetFiles(rootPath, "*.*", SearchOption.TopDirectoryOnly);
            bool shouldDeleteFolder = true;

            foreach (var file in files)
            {
                var lastAccess = File.GetLastAccessTimeUtc(file);
                if (now - lastAccess <= TimeSpan.FromMinutes(inactiveMinutes))
                {
                    shouldDeleteFolder = false;
                    break;
                }
            }

            if (!shouldDeleteFolder)
            {
                return;
            }

            var directories = Directory.GetDirectories(rootPath);
            foreach (var directory in directories)
            {
                CleanupOldFilesAndFolders(directory, inactiveMinutes);
            }

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Log.Error($"Không thể xóa file: {file} - {ex.Message}");
                }
            }

            if (Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories).Length == 0 &&
                Directory.GetDirectories(rootPath).Length == 0)
            {
                try
                {
                    Directory.Delete(rootPath, true);
                }
                catch (Exception ex)
                {
                    Log.Error($"Không thể xóa thư mục: {rootPath} - {ex.Message}");
                }
            }
        }
    }
}
