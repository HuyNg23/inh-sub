using IBox.Common.FolderLog;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[IBoxRootAuthorization]
    [IBoxRootTenantAuthorization]
    public class LogController : ControllerBase
    {
        private readonly IRestLog _restLog;
        public LogController(IRestLog restLog)
        {
            _restLog = restLog;
        }

        [HttpGet("Download/{filename}")]
        public IActionResult DownloadFromLocal(string fileName)
        {
            FileStream reader = new FileStream(Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}Logs{Path.DirectorySeparatorChar}" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            return File(reader, "application/force-download", fileName);
        }

        [HttpGet("Downloads/{service}/{filename}")]
        public IActionResult Download(IboxService service, string fileName)
        {
            List<LogInfo> logInfos = new List<LogInfo>();
            var zipName = fileName + $".zip";
            using (MemoryStream ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    List<string> servers = new List<string>();
                    switch (service)
                    {
                        case IboxService.RootServersIBox:
                            servers.AddRange(IBGlobalConfig.RootServersIBox);
                            break;

                        case IboxService.ServersIBox:
                            servers.AddRange(IBGlobalConfig.ServersIBox);
                            break;

                        case IboxService.RestServiceIBox:
                            servers.AddRange(IBGlobalConfig.RestServiceIBox);
                            break;

                        case IboxService.ScheduleServiceIBox:
                            servers.AddRange(IBGlobalConfig.ScheduleServiceIBox);
                            break;

                        case IboxService.LogService:
                            servers.AddRange(IBGlobalConfig.LogServiceIBox);
                            break;

                        case IboxService.ChatBotService:
                            servers.AddRange(IBGlobalConfig.ChatBotServiceIBox);
                            break;

                        case IboxService.StoreService:
                            servers.AddRange(IBGlobalConfig.StoreService);
                            break;

                        default:
                            break;
                    }

                    servers.ToList().ForEach(host =>
                    {
                        var rest = string.Format(@"{0}{1}{2}", host, "/api/Log/Download/", fileName);
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage();
                        HttpResponseMessage httpResponse = new HttpResponseMessage();
                        requestMessage = new HttpRequestMessage(HttpMethod.Get, rest);

                        requestMessage.Content = new StringContent(@"{}", Encoding.UTF8, "Application/json");
                        var authorization = HttpContext.Request.Headers["Authorization"];
                        requestMessage.Headers.Add("Authorization", authorization.ToString());
                        try
                        {
                            httpResponse = httpClient.SendAsync(requestMessage).Result;
                            if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var entry = zip.CreateEntry(string.Format("{0}{1}", host, fileName));
                                using (var fileStream = new MemoryStream(httpResponse.Content.ReadAsByteArrayAsync().Result))
                                {
                                    using (var entryStream = entry.Open())
                                    {
                                        fileStream.CopyTo(entryStream);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message, ex);
                        }
                    });
                }
                return File(ms.ToArray(), "application/zip", zipName);
            }
        }

        [HttpGet("ShowList")]
        public ResponseForm<List<LogInfo>> ListLog()
        {
            return new ResponseForm<List<LogInfo>>(() =>
            {
                return _restLog.ListLog("Client");
            });
        }

        [HttpGet("ShowAllLog/{service}")]
        public ResponseForm<List<LogInfo>> ShowAllLog(IboxService service)
        {
            return new ResponseForm<List<LogInfo>>(() =>
            {
                List<LogInfo> logInfos = new List<LogInfo>();
                List<string> servers = new List<string>();
                switch (service)
                {
                    case IboxService.RootServersIBox:
                        servers.AddRange(IBGlobalConfig.RootServersIBox);
                        break;

                    case IboxService.ServersIBox:
                        servers.AddRange(IBGlobalConfig.ServersIBox);
                        break;

                    case IboxService.RestServiceIBox:
                        servers.AddRange(IBGlobalConfig.RestServiceIBox);
                        break;

                    case IboxService.ScheduleServiceIBox:
                        servers.AddRange(IBGlobalConfig.ScheduleServiceIBox);
                        break;

                    case IboxService.LogService:
                        servers.AddRange(IBGlobalConfig.LogServiceIBox);
                        break;

                    case IboxService.ChatBotService:
                        servers.AddRange(IBGlobalConfig.ChatBotServiceIBox);
                        break;

                    case IboxService.StoreService:
                        servers.AddRange(IBGlobalConfig.StoreService);
                        break;

                    default:
                        break;
                }

                var Authorize = HttpContext.Request.Headers["Authorization"];
                servers.ToList().ForEach(ip =>
                {
                    var rest = string.Format(@"{0}{1}", ip, "/api/Log/ShowList");
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                    HttpClient httpClient = new HttpClient(handler);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, rest);

                    requestMessage.Content = new StringContent(@"{}", Encoding.UTF8, "Application/json");
                    requestMessage.Headers.Add("Authorization", Authorize.ToString());
                    try
                    {
                        HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var listLog = httpResponse.Content.ReadFromJsonAsync<ResponseForm<List<LogInfo>>>().Result;
                            if (listLog != null && listLog.Data != null)
                            {
                                listLog.Data.ForEach(ptr => ptr.Location += " side: " + ip.Split(".").LastOrDefault());
                                logInfos.AddRange(listLog.Data);
                            }
                        }
                    }
                    catch
                    {
                        Log.Error(string.Format("Can not connect to server {0}", ip.Split("//")[1]));
                    }
                });

                return logInfos.OrderByDescending(ptr => ptr.CreatedDate).ToList();
            });
        }
    }
}