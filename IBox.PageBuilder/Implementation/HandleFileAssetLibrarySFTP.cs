using IBox.Database.Root.Tables;
using IBox.PageBuilder.Factories;
using Renci.SshNet.Common;
using Renci.SshNet;
using IBox.PageBuilder.Model;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using IBox.Common.Chat.CommonData.CallData;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using System.Net;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Common.Security;
using Org.BouncyCastle.Ocsp;
using Newtonsoft.Json;
using FluentFTP;
using System.Text;

namespace IBox.PageBuilder.Implementation
{
    public class HandleFileAssetLibrarySFTP : IHandleFileAssetLibrarySFTP
    {
        private readonly ICommonData _commonData;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IHandleFileAndDB _handleFileAndDB;
        public HandleFileAssetLibrarySFTP(ICommonData commonData, IHandleFileAndDB handleFileAndDB, IConfiguration configuration, IEncryption encryption)
        {
            _commonData = commonData;
            _handleFileAndDB = handleFileAndDB;
            _configuration = configuration;
            _encryption = encryption;
        }
        public byte[] GetFileFromSFTP(string fileName, string tenantId, A_AssetLibraryStorage storageConfig)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(fileName))
                    throw new ArgumentNullException(nameof(fileName));
                if (storageConfig == null)
                    throw new ArgumentNullException(nameof(storageConfig));

                // Tạo kết nối SFTP
                using var sftp = new SftpClient(
                    host: storageConfig.Host,
                    port: int.Parse(storageConfig.Port), // Thường là 22 cho SFTP
                    username: storageConfig.UserName,
                    password: storageConfig.Password
                );

                sftp.Connect(); // SSH.NET chưa có API async native

                string filePath = Path.Combine(storageConfig.PathStorage, "assets", tenantId, fileName).Replace("\\", "/");

                // Kiểm tra file tồn tại
                if (!sftp.Exists(filePath))
                {
                    throw new FileNotFoundException($"File '{fileName}' not found at path: {filePath}");
                }

                // Tải file
                using var memoryStream = new MemoryStream();
                sftp.DownloadFile(filePath, memoryStream);

                return memoryStream.ToArray();
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (SshAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("SFTP authentication failed", ex);
            }
            catch (SshException ex)
            {
                throw new IOException($"SFTP operation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error downloading file from SFTP: {ex.Message}", ex);
            }

        }

        public string UploadFileToSFTP(ReqUpLoadAssetLibrary req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            if (req?.Files == null || req.Files.Count == 0)
                throw new ArgumentNullException(nameof(req.Files), "File list cannot be null or empty");

            if (storageConfig == null)
                throw new ArgumentNullException(nameof(storageConfig), "Storage configuration cannot be null");

            try
            {
                using var sftp = new SftpClient(storageConfig.Host, int.Parse(storageConfig.Port),
                    storageConfig.UserName, storageConfig.Password);
                sftp.Connect();

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
                        fullPath = _commonData.CreateFolderSFTP(sftp, fullPath);
                        uploadedPathFiles.Add(fullPath);

                        using var fileStream = file.OpenReadStream();
                        if (!fileStream.CanRead)
                            throw new IOException($"Cannot read stream for file '{file.FileName}'");

                        sftp.UploadFile(fileStream, fullPath, true);
                        if (!sftp.Exists(fullPath))
                            throw new IOException($"File '{file.FileName}' was not uploaded successfully to {fullPath}");

                        uploadedFiles.Add(fullPath);
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
                        _handleFileAndDB.AddOrUpdateAssetLibraryDb(req.Name, uploadedFiles[0], req.Version, Path.GetExtension(uploadedFiles[0]), tenantId);
                        uploadedFiles.Add($"Saved {uploadedFiles[0]} to database successfully.");
                    }
                    catch (Exception ex)
                    {
                        uploadedFiles.Add($"Failed to save {uploadedFiles[0]} to database: {ex.Message}");
                        RollbackUploadedFiles(sftp, uploadedPathFiles);
                    }
                }
                else
                {
                    RollbackUploadedFiles(sftp, uploadedPathFiles);
                }

                return $"Successfully uploaded {uploadedFiles.Count} files to SFTP: {string.Join(", ", uploadedFiles)}";
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (SshAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("SFTP authentication failed", ex);
            }
            catch (SshException ex)
            {
                throw new IOException($"SFTP upload failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading files to SFTP: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Rollback các file đã upload nếu xảy ra lỗi
        /// </summary>
        private void RollbackUploadedFiles(SftpClient sftp, List<string> uploadedPathFiles)
        {
            foreach (var path in uploadedPathFiles)
            {
                _handleFileAndDB.DeleteFileSFTP(sftp, path);
            }
        }

        public bool DeleteFileAssetLibrarySFTP(A_AssetLibraryStorage storageConfig, string id, string authorization, string tenantId)
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

            DeleteFileFromSFTP(assetLibrary.Url, storageConfig, tenantId);

            _handleFileAndDB.DeleteAssetLibraryOnDb(id, tenantId);

            return true;
        }
        public void DeleteFileFromSFTP(string pathName, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            try
            {
                if (string.IsNullOrEmpty(pathName))
                    throw new ArgumentNullException(nameof(pathName));
                if (storageConfig == null)
                    throw new ArgumentNullException(nameof(storageConfig));

                using var sftp = new SftpClient(
                    host: storageConfig.Host,
                    port: int.Parse(storageConfig.Port),
                    username: storageConfig.UserName,
                    password: storageConfig.Password
                );

                sftp.Connect();
                string filePath = Path.Combine(storageConfig.PathStorage, "assets", tenantId, pathName).Replace("\\", "/");
                if (!sftp.Exists(filePath))
                {
                    throw new FileNotFoundException($"File '{pathName}' not found at path: {filePath}");
                }

                sftp.DeleteFile(filePath);
                if (sftp.Exists(filePath))
                {
                    throw new IOException($"Failed to delete file '{pathName}' at {filePath}");
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Invalid port number format", nameof(storageConfig.Port), ex);
            }
            catch (SshAuthenticationException ex)
            {
                throw new UnauthorizedAccessException("SFTP authentication failed", ex);
            }
            catch (SshException ex)
            {
                throw new IOException($"SFTP delete operation failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting file from SFTP: {ex.Message}", ex);
            }
        }
        public bool SaveFileHTMLSFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            using var sftp = new SftpClient(
                   host: storageConfig.Host,
                   port: int.Parse(storageConfig.Port),
                   username: storageConfig.UserName,
                   password: storageConfig.Password
               );

            try
            {
                sftp.Connect();
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

                    string fullPath = _commonData.CreateFolderSFTP(sftp, path);

                    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                    sftp.UploadFile(ms, fullPath);
                }

                req.CSS = Path.Combine("CSS", $"{req.Name}.css");
                req.HTML = Path.Combine("HTML", $"{req.Name}.html");
                req.Library = Path.Combine("Library", $"{req.Name}.txt");
                req.JavaScript = Path.Combine("JavaScript", $"{req.Name}.js");
                _handleFileAndDB.UpdateSaveFileComponentsDB(req, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"SaveFileHTMLSFTP: {ex.Message} \n Request: {JsonConvert.SerializeObject(req)}");
                return false;
            }
            finally
            {
                if (sftp.IsConnected)
                {
                    sftp.Disconnect();
                }
            }
        }
        public ResWebResource GetAllFileHtmlSFTP(ReqWebResource req, A_AssetLibraryStorage storageConfig, string tenantId)
        {
            try
            {
                string basePath = _handleFileAndDB.NormalizePath(storageConfig.PathStorage);
                string relativePath = Path.Combine(basePath, "components", tenantId, req.Id);
                string localBasePath = Path.Combine(Directory.GetCurrentDirectory(), "components", tenantId, req.Id);
                string localBasePathTenant = Path.Combine(Directory.GetCurrentDirectory(), "components", tenantId).Replace("\\", "/");

                SftpClient sftp = null;
                bool sftpAvailable = true;

                try
                {
                    sftp = new SftpClient(
                           host: storageConfig.Host,
                           port: int.Parse(storageConfig.Port),
                           username: storageConfig.UserName,
                           password: storageConfig.Password
                             );

                    sftp.Connect();
                }
                catch (Exception ex)
                {
                    sftpAvailable = false;
                    Log.Error($"Không thể kết nối FTP: {ex.Message} → sẽ đọc từ local.");
                }

                var result = new ResWebResource
                {
                    Id = req.Id,
                    Name = req.Name,
                    CSS = ReadFileWithFallback(sftp, sftpAvailable, relativePath, localBasePath, "CSS", req.Name, ".css", req.CheckNew),
                    HTML = ReadFileWithFallback(sftp, sftpAvailable, relativePath, localBasePath, "HTML", req.Name, ".html", req.CheckNew),
                    Library = ReadFileWithFallback(sftp, sftpAvailable, relativePath, localBasePath, "Library", req.Name, ".txt", req.CheckNew),
                    JavaScript = ReadFileWithFallback(sftp, sftpAvailable, relativePath, localBasePath, "JavaScript", req.Name, ".js", req.CheckNew)
                };

                if (sftpAvailable)
                {
                    if (sftp != null)
                    {
                        sftp.Disconnect();
                    }
                }

                CleanupOldFilesAndFolders(localBasePathTenant, 10);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"GetAllFileHtmlSFTP: {ex.Message}");
                throw;
            }
        }
        public string ReadFileWithFallback(SftpClient? sftp, bool ftpAvailable, string remoteRoot, string localRoot, string subFolder, string fileName, string extension, bool checkNew)
        {
            string remotePath = Path.Combine(remoteRoot, subFolder, fileName + extension).Replace("\\", "/");
            string localPath = Path.Combine(localRoot, subFolder, fileName + extension).Replace("\\", "/");

            try
            {
                if (!ftpAvailable || sftp == null)
                {
                    if(File.Exists(localPath))
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

                if (File.Exists(localPath) && checkNew == false)
                {
                    return File.ReadAllText(localPath, Encoding.UTF8);
                }

                try
                {
                    return DownloadAndReadFile(sftp, remotePath, localPath);
                }
                catch (Exception ex)
                {
                    Log.Error($"Lỗi khi tải file từ SFTP: {remotePath}, Exception: {ex.Message}");
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error($"Lỗi khi đọc file {remotePath}: {ex.Message}");
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

        public string DownloadAndReadFile(SftpClient sftp, string remoteFilePath, string localFilePath)
        {
            try
            {
                using (var fileStream = new FileStream(localFilePath, FileMode.Create))
                {
                    sftp.DownloadFile(remoteFilePath, fileStream);
                }

                string fileContent = File.ReadAllText(localFilePath, Encoding.UTF8);
                return fileContent;
            }
            catch (Exception ex)
            {
                Log.Error($"DownloadFile: {ex.Message}");
                throw;
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
