using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IBox.Client.Controllers.ChatBot
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class DataChatController : ControllerBase
    {
        [HttpGet("Downloads/{service}/{filename}/{date}")]
        [IBoxActionPermission("integration-config-chatbot-manage", "integration -config-chatbot-download")]
        public IActionResult Download(IboxService service, string fileName, string date)
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
                        case IboxService.LogService:
                            servers.AddRange(IBGlobalConfig.LogServiceIBox);
                            break;

                        default:
                            break;
                    }

                    servers.ToList().ForEach(host =>
                    {
                        var rest = string.Format(@"{0}{1}{2}/{3}", host, "/api/DataChatBot/Download/", fileName, date);
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
                            new IboxLog(ex.Message, "AppLogs", ex);
                        }
                    });
                }
                return File(ms.ToArray(), "application/zip", zipName);
            }
        }
    }
}