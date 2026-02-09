using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;

namespace IBox.Client.Business.Model
{
    public class BDynamicLinkedLibrary : BBaseBusiness<BDynamicLinkedLibrary>
    {
        private IFormFile? file;
        private string? id;
        private string? name;
#pragma warning disable CS0414 // The field 'BDynamicLinkedLibrary.link' is assigned but its value is never used
        private string? link;
#pragma warning restore CS0414 // The field 'BDynamicLinkedLibrary.link' is assigned but its value is never used
        private string? config;
        private string? functionList;
        private string? fileName;
        private string? groupId;
        private DateTime? createdDate;

        public string? Id { get => id; set => id = value; }
        public IFormFile? File { get => file; set => file = value; }
        public string? Name { get => name; set => name = value; }
        public string? Config { get => config; set => config = value; }
        public string? FunctionList { get => functionList; set => functionList = value; }
        public string? FileName { get => fileName; set => fileName = value; }
        public DateTime? CreatedDate { get => createdDate; set => createdDate = value; }
        public string? GroupId { get => groupId; set => groupId = value; }

        /// <summary>
        /// save file dll mới được đẩy lên
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string SaveFile(HttpRequest requestContext)
        {
            try
            {
                if (File == null)
                {
                    throw new IboxLog("Not exist file in request", tenantContext.Context.TenantInfo.Id);
                }

                if (string.IsNullOrEmpty(name?.Trim()))
                {
                    throw new IboxLog("Name can't null or empty", tenantContext.Context.TenantInfo.Id);
                }


                var dynamicLinkModel = this.tenantContext.Context.DynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Name == name && !ptr.IsDelete);


                if (dynamicLinkModel != null)
                {
                    throw new IboxLog("Name Dll already exist.", tenantContext.Context.TenantInfo.Id);
                }

                var rootDir = Directory.GetCurrentDirectory();
                var folderName = Path.Combine("DLL");
                var pathToSave = Path.Combine(rootDir, folderName);
                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }

                if (File.Length > 0)
                {
                    var fileNameX = ContentDispositionHeaderValue.Parse(File.ContentDisposition).FileName?.Trim('"');
                    if (fileNameX == null)
                    {
                        throw new IboxLog("can not create filename", tenantContext.Context.TenantInfo.Id);
                    }

                    if (Path.GetExtension(fileNameX) != ".dll")
                    {
                        throw new IboxLog("Extension of file must be DLL", tenantContext.Context.TenantInfo.Id);
                    }

                    fileNameX = Guid.NewGuid() + Path.GetExtension(fileNameX);
                    var fullPath = Path.Combine(pathToSave, fileNameX);
                    var dbPath = Path.Combine(folderName, fileNameX);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        File.CopyTo(stream);
                    }
                    this.tenantContext.Context.DynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Name == name && !ptr.IsDelete).TryCreate(this.tenantContext.Context, new DynamicLinkedLibrary()
                    {
                        Name = name,
                        Config = this.config,
                        Link = dbPath,
                        FunctionList = functionList,
                        GroupId = groupId,
                    });

                    var configs = this.Configuration.Config;
                    if (configs == null)
                    {
                        throw new IboxLog("Can not load config", tenantContext.Context.TenantInfo.Id);
                    }

                    var Authorize = requestContext.Headers["Authorization"];
                    var tenantID = requestContext.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                    List<string> services = new List<string>();
                    services.AddRange(IBGlobalConfig.ServersIBox);
                    services.AddRange(IBGlobalConfig.RootServersIBox);
                    services.AddRange(IBGlobalConfig.RestServiceIBox);
                    services.AddRange(IBGlobalConfig.ScheduleServiceIBox);
                    services.AddRange(IBGlobalConfig.LogServiceIBox);
                    services.AddRange(IBGlobalConfig.ChatBotServiceIBox);

                    services.ForEach(e =>
                    {
                        Task.Run(() =>
                            {
                                try
                                {
                                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                                    WebClient webClient = new WebClient();
                                    webClient.Headers.Add("Authorization", Authorize.ToString());
                                    webClient.Headers.Add("Tenant", tenantID);
                                    webClient.UploadFile($"{e}/api/DLL/UploadFile", "POST", fullPath);
                                    webClient.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    new IboxLog($"Can't UploadFile to Server: {e} \n With Error {ex.Message}", tenantID, "Error");
                                }
                            });
                    });

                    return dbPath;
                }
                else
                {
                    throw new IboxLog("File is empty", tenantContext.Context.TenantInfo.Id);
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", tenantContext?.Context?.TenantInfo?.Id ?? "AppLogs", ex);
            }
        }

        public string SaveFile(IFormFile fileCoppy)
        {
            try
            {
                if (fileCoppy == null)
                {
                    throw new IboxLog("Not exist file in request", "AppLogs");
                }

                var rootDir = Directory.GetCurrentDirectory();
                var folderName = Path.Combine("DLL");
                var pathToSave = Path.Combine(rootDir, folderName);
                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }

                if (fileCoppy.Length > 0)
                {
                    var fileNameX = ContentDispositionHeaderValue.Parse(fileCoppy.ContentDisposition).FileName?.Trim('"');
                    if (fileNameX == null)
                    {
                        throw new IboxLog("can not create filename", "AppLogs");
                    }

                    if (Path.GetExtension(fileNameX) != ".dll")
                    {
                        throw new IboxLog("Extension of file must be DLL", "AppLogs");
                    }
                    var fullPath = Path.Combine(pathToSave, fileNameX);
                    var dbPath = Path.Combine(folderName, fileNameX);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        fileCoppy.CopyTo(stream);
                    }

                    return dbPath;
                }
                else
                {
                    throw new IboxLog("File is empty", "AppLogs");
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        public void SyncFileOnRing(HttpRequest requestContext)
        {
            try
            {
                var rootDir = Directory.GetCurrentDirectory();
                var folderName = Path.Combine("DLL");
                var pathToSave = Path.Combine(rootDir, folderName);
                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }

                foreach (var fileItem in Directory.GetFiles(pathToSave))
                {
                    var appConfig = this.Configuration.Config;
                    if (appConfig == null)
                    {
                        throw new IboxLog("Can not load config", "AppLogs");
                    }
                    var Authorize = requestContext.Headers["Authorization"];

                    List<string> services = new List<string>();
                    services.AddRange(IBGlobalConfig.ServersIBox);
                    services.AddRange(IBGlobalConfig.RootServersIBox);
                    services.AddRange(IBGlobalConfig.RestServiceIBox);
                    services.AddRange(IBGlobalConfig.ScheduleServiceIBox);
                    services.AddRange(IBGlobalConfig.LogServiceIBox);
                    services.AddRange(IBGlobalConfig.ChatBotServiceIBox);

                    services.ForEach(ip =>
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });
                                using (var webClient = new WebClient())
                                {
                                    webClient.Headers.Add("Authorization", Authorize.ToString());
                                    webClient.UploadFile($"{ip}/api/DLL/RootRequestSyncFileHA", "POST", fileItem);
                                }
                            }
                            catch (Exception ex)
                            {
                                new IboxLog($"Can't UploadFile to Server: {ip} \n With Error {ex.Message}", "AppLogs", "Error");
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        public void CallSyncFileOnRing(HttpRequest requestContext)
        {
            try
            {
                var rootDir = Directory.GetCurrentDirectory();
                var folderName = Path.Combine("DLL");
                var pathSave = Path.Combine(rootDir, folderName);
                if (!Directory.Exists(pathSave))
                {
                    return;
                }

                var Authorize = requestContext.Headers["Authorization"];
                var appConfig = this.Configuration.Config;
                if (appConfig == null)
                {
                    throw new IboxLog("Can not load config", "AppLogs");
                }

                List<string> services = new List<string>();
                services.AddRange(IBGlobalConfig.ServersIBox);
                services.AddRange(IBGlobalConfig.RootServersIBox);
                services.AddRange(IBGlobalConfig.RestServiceIBox);
                services.AddRange(IBGlobalConfig.ScheduleServiceIBox);
                services.AddRange(IBGlobalConfig.ChatBotServiceIBox);
                services.AddRange(IBGlobalConfig.LogServiceIBox);

                services.ForEach(ip =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            try
                            {
                                var handler = new HttpClientHandler();
                                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                                using (var httpClient = new HttpClient(handler))
                                {
                                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{ip}/api/DLL/SyncFileHA");
                                    requestMessage.Headers.Add("Authorization", Authorize.ToString());
                                    httpClient.SendAsync(requestMessage);
                                    Thread.Sleep(300);
                                }
                            }
                            catch (Exception ex)
                            {
                                new IboxLog(ex.Message, "AppLogs", ex);
                            }
                        }
                        catch (Exception ex)
                        {
                            new IboxLog($"Can't UploadFile to Server: {ip} \n With Error {ex.Message}", "AppLogs", ex);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", "AppLogs", ex);
            }
        }

        /// <summary>
        /// Lấy danh sách dll
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public List<BDynamicLinkedLibrary> GetAllDll()
        {
            try
            {


                return this.tenantContext.Context.DynamicLinkedLibraries
                    .Where(ptr => !ptr.IsDelete)
                    .ToList()
                    .Select(ptr => new BDynamicLinkedLibrary()
                    {
                        Id = ptr.Id ?? string.Empty,
                        Name = ptr.Name ?? string.Empty,
                        config = ptr.Config ?? string.Empty,
                        link = ptr.Link ?? string.Empty,
                        FunctionList = ptr.FunctionList,
                        FileName = ptr.Link.Split(Path.DirectorySeparatorChar).LastOrDefault(),
                        CreatedDate = ptr.CreatedDate,
                        GroupId = ptr.GroupId == null ? string.Empty : ptr.GroupId,
                    })
                    .OrderByDescending(ptr => ptr.CreatedDate)
                    .ToList();


            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", this.tenantContext?.Context?.TenantInfo?.Id ?? "AppLogs", ex);
            }
        }

        /// <summary>
        /// Lấy chi tiết cấu hình dll
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public BDynamicLinkedLibrary GetDetail()
        {
            try
            {

                var dll = this.tenantContext.Context.DynamicLinkedLibraries
                    .Where(ptr => !ptr.IsDelete && ptr.Id == Id)
                    .Select(ptr => new BDynamicLinkedLibrary()
                    {
                        Name = ptr.Name,
                        config = ptr.Config,
                        link = ptr.Link,
                        FunctionList = ptr.FunctionList,
                        GroupId = ptr.GroupId,
                    }).FirstOrDefault();


                if (dll == null)
                {
                    return new BDynamicLinkedLibrary();
                }

                return dll;
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", this.tenantContext?.Context?.TenantInfo?.Id ?? "AppLogs", ex);
            }
        }

        /// <summary>
        /// Cập nhật cấu hình cho dll
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void UpdateDllConfig()
        {
            try
            {
                if (string.IsNullOrEmpty(this.id))
                {
                    throw new IboxLog("id of dll is required", tenantContext.Context.TenantInfo.Id);
                }


                var DynamicLinkModel = this.tenantContext.Context.DynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Name == this.Name && ptr.Id != this.Id);


                if (DynamicLinkModel != null)
                {
                    throw new IboxLog("Name dll already exist", tenantContext.Context.TenantInfo.Id);
                }

                this.tenantContext.Context.DynamicLinkedLibraries.FirstOrDefault(ptr => ptr.Id == this.id).TryUpdate(this.tenantContext.Context, new DynamicLinkedLibrary()
                {
                    Id = this.id,
                    Name = name,
                    Config = config,
                    FunctionList = functionList,
                    GroupId = groupId,
                });
            }
            catch (Exception ex)
            {
                throw new IboxLog($"An Unexpected Error Has Occurred: {ex.Message}", this.tenantContext?.Context?.TenantInfo?.Id ?? "AppLogs", ex);
            }
        }
    }
}