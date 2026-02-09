using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Vml.Office;
using FluentFTP;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Objects;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using Renci.SshNet;
using Serilog;
using System;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace IBox.Common.Chat.Configuration
{
    public class CommonData : ICommonData
    {
        public DateTime MIN_DATE = new DateTime(1950, 1, 1);
        public DateTime MAX_DATE = new DateTime(9999, 1, 1);

        /// <summary>
        /// Tạo nhiều thư mục khi chưa tồn tại
        /// </summary>
        /// <param name="PathLocal">đường dẫn lưu file VD: D:\Project\TPBank1\Webhook_new1\webhooks1\ChatBot1\FileName.txt</param>
        public int ShowSecond()
        {
            int seconds = DateTime.Now.Second;
            return (seconds / 15) * 15;
        }

        public long ConvertDateTimeToTimeSpan(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
        }

        public DateTime ConvertTimeSpanToDateTime(long unixTimestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime.ToLocalTime();
        }

        public void ExtractFileZip(string fullName, string extractPath)
        {
            const int MAX_SUPPORTED_SIZE = 100 * 1024 * 1024; // 100MB
            const int MAX_ENTRIES = 1000;

            try
            {
                using var archive = ZipFile.OpenRead(fullName);

                if (archive.Entries.Count > MAX_ENTRIES)
                    throw new InvalidOperationException("Too many entries in zip file");

                foreach (var entry in archive.Entries)
                {
                    var sanitizedPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName))
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    if (!sanitizedPath.StartsWith(extractPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal))
                    {
                        new IboxLog($"Blocked path traversal attempt: {entry.FullName}", "AppLogs", "Info");
                        continue;
                    }

                    if (entry.FullName.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(sanitizedPath);
                        continue;
                    }

                    if (entry.CompressedLength > MAX_SUPPORTED_SIZE || entry.Length > MAX_SUPPORTED_SIZE)
                    {
                        new IboxLog($"File {entry.FullName} exceeds size limit", "AppLogs", "Info");
                        continue;
                    }

                    var parentDir = Path.GetDirectoryName(sanitizedPath);
                    if (!Directory.Exists(parentDir))

                        Directory.CreateDirectory(parentDir);

                    using (var fs = new FileStream(sanitizedPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.DeleteOnClose))
                    using (var entryStream = entry.Open())
                    {
                        entryStream.CopyTo(fs);
                        fs.Flush();
                        File.SetAttributes(sanitizedPath, FileAttributes.Normal);
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"Extraction failed: {ex.Message}", "AppLogs", "Error");
                throw;
            }
        }

        public void ExtractFileZipPattern(string fullName, string extractPath, string pattern)
        {
            const int MAX_SUPPORTED_SIZE = 100 * 1024 * 1024; // 100MB
            const int MAX_ENTRIES = 1000;

            try
            {
                using var archive = ZipFile.OpenRead(fullName);

                if (archive.Entries.Count > MAX_ENTRIES)
                    throw new InvalidOperationException("Too many entries in zip file");

                var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)); // có timeout

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/") || string.IsNullOrEmpty(entry.Name))
                        continue; // thư mục hoặc entry rỗng

                    string fileName = Path.GetFileName(entry.FullName);

                    // kiểm tra pattern
                    bool isMatch;
                    try
                    {
                        isMatch = regex.IsMatch(fileName);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        new IboxLog($"Regex timeout với file: {fileName}", "AppLogs", "Info");
                        continue;
                    }

                    if (!isMatch)
                    {
                        new IboxLog($"Bỏ qua file không khớp pattern: {fileName}", "AppLogs", "Info");
                        continue;
                    }

                    if (entry.CompressedLength > MAX_SUPPORTED_SIZE || entry.Length > MAX_SUPPORTED_SIZE)
                    {
                        new IboxLog($"File {entry.FullName} exceeds size limit", "AppLogs", "Info");
                        continue;
                    }

                    var sanitizedPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName))
                        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    if (!sanitizedPath.StartsWith(
                            extractPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                            StringComparison.Ordinal))
                    {
                        new IboxLog($"Blocked path traversal attempt: {entry.FullName}", "AppLogs", "Info");
                        continue;
                    }

                    var parentDir = Path.GetDirectoryName(sanitizedPath);
                    if (!Directory.Exists(parentDir))
                        Directory.CreateDirectory(parentDir);

                    using (var fs = new FileStream(sanitizedPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.DeleteOnClose))
                    using (var entryStream = entry.Open())
                    {
                        entryStream.CopyTo(fs);
                        fs.Flush();
                        File.SetAttributes(sanitizedPath, FileAttributes.Normal);
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"Extraction failed: {ex.Message}", "AppLogs", "Error");
                throw;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="PathLocal">đường dẫn lưu file VD: D:\Project\TPBank1\Webhook_new1\webhooks1\ChatBot\htmm.txt</param>
        public void CreateFolder(string PathLocal)
        {
            try
            {
                string[] folders = SplitFolder(PathLocal);

                string newPath = PathLocal.StartsWith("/") ? "/" : string.Empty;

                for (int i = 0; i < folders.Length - 1; i++)
                {
                    newPath = string.IsNullOrEmpty(newPath) ? folders[i] : Path.Combine(newPath, folders[i]);

                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog("Error create folder\n" + ex.Message, "AppLogs", "Error");
            }
            return;
        }

        public string formatDataStringSQL(string? data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    return "";
                }

                return data.Replace("'", "''");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Dictionary<string, string> GetColumnsFromModelSQLite<T>()
        {
            var columns = new Dictionary<string, string>();

            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                string columnName = property.Name;
                string columnType;

                if (property.PropertyType == typeof(string))
                {
                    columnType = "TEXT";
                }
                else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long))
                {
                    columnType = "INTEGER";
                }
                else if (property.PropertyType == typeof(float) || property.PropertyType == typeof(double) || property.PropertyType == typeof(decimal))
                {
                    columnType = "REAL";
                }
                else if (property.PropertyType == typeof(bool))
                {
                    columnType = "INTEGER";
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    columnType = "DATETIME";
                }
                else
                {
                    columnType = "TEXT";
                }

                columns.Add(columnName, columnType);
            }

            return columns;
        }

        /// <summary>
        /// Tạo nhiều thư mục khi chưa tồn tại
        /// </summary>
        /// <param name="PathLocal">đường dẫn lưu file VD: D:\Project\TPBank1\Webhook_new1\webhooks1\ChatBot</param>
        public void CreateFolder1(string pathLocal)
        {
            try
            {
                string[] folders = SplitFolder(pathLocal);
                string newPath = pathLocal.StartsWith("/") ? "/" : string.Empty;

                foreach (string folder in folders)
                {
                    newPath = string.IsNullOrEmpty(newPath) ? folder : Path.Combine(newPath, folder);

                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error create folder\n" + ex.Message);
            }
        }

        /// <summary>
        /// Tạo nhiều thư mục khi chưa tồn tại
        /// </summary>
        /// <param name="PathLocal">đường dẫn lưu file VD: D:\Project\TPBank1\Webhook_new1\webhooks1\ChatBot</param>

        public string CreateFolderSFTP1(SftpClient sftp, string pathLocal)
        {
            try
            {
                pathLocal = pathLocal.Replace("\\", "/");

                string[] folders = pathLocal.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string newPath = pathLocal.StartsWith("/") ? "/" : string.Empty;

                for (int i = 0; i < folders.Length; i++)
                {
                    newPath += (newPath == "/" ? "" : "/") + folders[i];

                    if (!sftp.Exists(newPath))
                    {
                        sftp.CreateDirectory(newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error create folder\n" + ex.Message);
            }

            return pathLocal;
        }
        /// <summary>
        /// Tạo nhiều thư mục khi chưa tồn tại
        /// </summary>
        /// <param name="pathLocal">đường dẫn lưu file VD: D:\Project\TPBank1\Webhook_new1\webhooks1\ChatBot\File.txt</param>

        public string CreateFolderSFTP(SftpClient sftp, string pathLocal)
        {
            try
            {
                pathLocal = pathLocal.Replace("\\", "/");

                string[] folders = pathLocal.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string newPath = pathLocal.StartsWith("/") ? "/" : string.Empty;

                for (int i = 0; i < folders.Length - 1; i++)
                {
                    newPath += (newPath == "/" ? "" : "/") + folders[i];

                    if (!sftp.Exists(newPath))
                    {
                        sftp.CreateDirectory(newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog("Error create folder\n" + ex.Message, "AppLogs", "Error");
            }

            return pathLocal;
        }
        public string CreateFolderFTP(FtpClient ftp, string pathLocal)
        {
            try
            {
                pathLocal = pathLocal.Replace("\\", "/");

                string[] folders = pathLocal.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string newPath = pathLocal.StartsWith("/") ? "/" : string.Empty;

                for (int i = 0; i < folders.Length - 1; i++)
                {
                    newPath += (newPath == "/" ? "" : "/") + folders[i];

                    if (!ftp.DirectoryExists(newPath))
                    {
                        ftp.CreateDirectory(newPath);
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog("Error create folder\n" + ex.Message, "AppLogs", "Error");
            }

            return pathLocal;
        }
        public string[] SplitFolder(string PathLocal)
        {
            return PathLocal.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string GetFilePath(string Parth, DateTime messageDate)
        {
            return Path.Combine(Parth,
                                messageDate.ToString("yyyy"),
                                messageDate.ToString("MM"),
                                messageDate.ToString("dd"));
        }

        public DateTime ToDate(string date)
        {
            try
            {
                try
                {
                    return DateTime.Parse(date, new CultureInfo("en-US", true));
                }
                catch
                {
                    return DateTime.Parse(date, new CultureInfo("fr-FR", true));
                }
            }
            catch
            {
                return MIN_DATE;
            }
        }

        public DateTime ToDate1(string? date)
        {
            try
            {
                if (date == null)
                {
                    return MIN_DATE;
                }

                try
                {
                    return DateTime.Parse(date, new CultureInfo("vi-VN", true));
                }
                catch
                {
                    return DateTime.Parse(date, new CultureInfo("fr-FR", true));
                }
            }
            catch
            {
                return MIN_DATE;
            }
        }

        public DateTime? ToDate2(object x)
        {
            string date = "";
            if (x != null)
            {

                date = x.ToString();

            }
            try
            {
                try
                {

                    return DateTime.Parse(date, new CultureInfo("en-US", true));

                }
                catch
                {

                    return DateTime.Parse(date, new CultureInfo("fr-FR", true));

                }
            }
            catch
            {
                return null;
            }
        }

        public DateTime ToDate3(object x)
        {
            string date = "";
            if (x != null)
            {

                date = x.ToString();

            }
            try
            {
                try
                {

                    return DateTime.Parse(date, new CultureInfo("en-US", true));

                }
                catch
                {

                    return DateTime.Parse(date, new CultureInfo("fr-FR", true));

                }
            }
            catch
            {
                return MIN_DATE;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sourcePath">Đường dẫn từ</param>
        /// <param name="destinationPath">Đường dẫn đến</param>
        public void CopyFile(string sourcePath, string destinationPath)
        {
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destinationPath, true);
            }
        }

        public void GetAllFile(string ParthLocal, string ParthDestin)
        {
            var lstShowDirector = Directory.GetDirectories(ParthLocal).ToList();

            foreach (var item in lstShowDirector)
            {
                string parth = ParthLocal + Path.DirectorySeparatorChar + item;
                CreateFolder1(ParthDestin + Path.DirectorySeparatorChar + item);
                DirectoryInfo directoryInfo = new DirectoryInfo(parth);
                List<FileInfo> files = directoryInfo.GetFiles().ToList();
                foreach (var file in files)
                {
                    CopyFile(file.FullName, ParthDestin + Path.DirectorySeparatorChar + item);
                }
            }
        }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public HttpResponseMessage? ExecuteAPI(string body, string Url, string tokenBOT)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            new IboxLog("ExecuteAPI: " + body + "\n Post Url: " + Url, "AppLogs", "Info");
            try
            {
                //Lấy token
                //HttpClientHandler clientHandler = new HttpClientHandler();
                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                var clientHandler = new HttpClientHandler();
                clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                HttpClient client1 = new HttpClient(clientHandler);
                {
                    //var userUpdateContact1 = Encoding.ASCII.GetBytes($"{_JsonModel.UserName}:{_JsonModel.Pass}");
                    client1.DefaultRequestHeaders.Clear();
                    if (!string.IsNullOrEmpty(tokenBOT))
                    {
                        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBOT);
                    }

                    var requestUpdateContact1 = new HttpRequestMessage();
                    requestUpdateContact1.Method = HttpMethod.Post;
                    requestUpdateContact1.RequestUri = new Uri(Url);
                    requestUpdateContact1.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    return client1.SendAsync(requestUpdateContact1).Result;
                }
            }
            catch (Exception exx)
            {
                new IboxLog("Error Call API Post" + exx.Message, "AppLogs");
                return null;
            }
        }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public ReponeUploadFileBOT? UploadFileToFPT(string Url, byte[] bytes, string filename, string token)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            try
            {
                new IboxLog($"Response_Upload_File: Url: {Url}, \n filename: {filename}", "AppLogs", "Info");

                using (var handler = new HttpClientHandler())
                {
                    //handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                    using (var client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var content = new MultipartFormDataContent();
                        var fileContent = new ByteArrayContent(bytes);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeMapping(filename));
                        content.Add(fileContent, "file", filename);

                        var request = new HttpRequestMessage(HttpMethod.Post, Url)
                        {
                            Content = content
                        };

                        var response = client.Send(request);
                        response.EnsureSuccessStatusCode();

                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        var result = JsonConvert.DeserializeObject<ReponeUploadFileBOT>(responseContent);

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog("UploadFile Error: " + ex.Message, "AppLogs");
                return null;
            }
        }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public string? CallApiPost(string url, string bodyJson, string userName, string pass)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            var clientHandler = new HttpClientHandler();
            clientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            HttpClient client1 = new HttpClient(clientHandler);
            {
                var token = Encoding.ASCII.GetBytes($"{userName}:{pass}");
                client1.DefaultRequestHeaders.Clear();
                client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(token));
                var requestUpdateContact1 = new HttpRequestMessage();
                requestUpdateContact1.Method = HttpMethod.Post;
                requestUpdateContact1.RequestUri = new Uri(url);
                requestUpdateContact1.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                //client1.Timeout = TimeSpan.FromSeconds(_JsonModel.TimeOut);
                string strResponseContact1 = "";
                try
                {
                    using (HttpResponseMessage responseCRMUpdateContact1 = client1.SendAsync(requestUpdateContact1).Result)
                    {
                        using (HttpContent content = responseCRMUpdateContact1.Content)
                        {
                            strResponseContact1 = content.ReadAsStringAsync().Result;
                            return strResponseContact1;
                        }
                    }
                }
                catch (Exception exx)
                {
                    new IboxLog($"CallApiPost: url: {url}\n bodyJson: {bodyJson}\n userName: {userName} \n pass: {pass} \n Exception: {exx.Message}", "AppLogs", "Error");
                    return null;
                }
            }
        }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        public byte[]? ConvertUrlToByte(string url)
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
            try
            {

                byte[] bytes = null;


                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                using (var client = new HttpClient(handler))
                {
                    using (var response = client.GetAsync(url).Result)
                    {
                        bytes = response.Content.ReadAsByteArrayAsync().Result;
                    }
                }
                return bytes;
            }
            catch (Exception ex)
            {
                new IboxLog("convert To Byte: " + ex.Message, "AppLogs", "Error");
                return null;
            }
        }

        public class ReponeUploadFileBOT
        {
            public string? ok { get; set; }
            public string? url { get; set; }
        }

        private readonly FileExtensionContentTypeProvider _provider = new FileExtensionContentTypeProvider();

        public string GetMimeMapping(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty", nameof(filename));


            if (_provider.TryGetContentType(filename, out string contentType))
            {
                return contentType;
            }


            // Default MIME type if none is found
            return "application/octet-stream";
        }

        public void CopyDirectory(string sourceDir, string destinationDir)
        {
            // Tạo thư mục đích nếu nó không tồn tại
            Directory.CreateDirectory(destinationDir);

            // Sao chép tất cả các tệp trong thư mục gốc
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            // Sao chép tất cả các thư mục con
            foreach (string folder in Directory.GetDirectories(sourceDir))
            {
                string destFolder = Path.Combine(destinationDir, Path.GetFileName(folder));
                CopyDirectory(folder, destFolder);
            }
        }



        public void SaveFileLogCreateFile(string body, string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                CreateFolder1(path);
                int valueSecond = ShowSecond();
                Task.Run(() =>
                {
                    for (int i = 0; i < 30; i++)
                    {
                        string filePath = Path.Combine(path, $"{DateTime.Now.ToString("yyyyMMddHHmm")}_{valueSecond}_{i}.wal");
                        try
                        {
                            using (FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                            {
                                fs.Seek(0, SeekOrigin.End);

                                using (var sw = new StreamWriter(fs))
                                {
                                    sw.WriteLine($"{body},");
                                    sw.Flush();
                                }
                            }
                            break;
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(10);
                            continue;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                new IboxLog($"SaveFileLog: {ex.Message}", "AppLogs", "Error");
            }
        }
    }
}