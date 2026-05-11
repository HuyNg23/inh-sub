using DocumentFormat.OpenXml.EMMA;
using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Objects;
using IBox.Common.ServiceIB;
using IBox.Database.Root;
using Serilog;
using System.Linq;
using System.Text;

namespace IBox.Database.Tenant.ServicesManager
{
    public class GetDataLogStream : IGetDataLogStream
    {
        public static CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ICommonData _commonData;
        private readonly object _lock = new object();

        public GetDataLogStream(ICommonData commonData)
        {
            _commonData = commonData;
        }

        private readonly HttpClient client = new HttpClient()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public void GetDataLog()
        {
            try
            {
                lock (_lock)
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = new CancellationTokenSource();
                }

                var token = _cts.Token; // Lấy token mới

                var tenantIds = IBGlobalConfig.Tenants;
                var restServiceIBox = IBGlobalConfig.RestServiceIBox;
                var scheduleServiceIBox = IBGlobalConfig.ScheduleServiceIBox;
                var apiEndpoints = new List<ApiEndpointInfo>();

                var restEndpoints = restServiceIBox
                    .Where(ptr => ptr.Contains(IBGlobalConfig.ThisSite))
                    .ToList();
                if (!restEndpoints.Any())
                {
                    restEndpoints = restServiceIBox.ToList();
                }

                foreach (var urlRestService in restEndpoints)
                {
                    string urlGetDataLog = string.Format(@"{0}{1}", urlRestService, UrlServiceIBConfig.UrlGetDataStream);
                    apiEndpoints.Add(new ApiEndpointInfo()
                    {
                        Url = urlGetDataLog
                    });
                }

                var scheduleEndpoints = scheduleServiceIBox
                    .Where(ptr => ptr.Contains(IBGlobalConfig.ThisSite))
                    .ToList();
                if (!scheduleEndpoints.Any())
                {
                    scheduleEndpoints = scheduleServiceIBox.ToList();
                }

                foreach (var urlScheduleService in scheduleEndpoints)
                {
                    string urlGetDataLog = string.Format(@"{0}{1}", urlScheduleService, UrlServiceIBConfig.UrlGetDataStream);
                    apiEndpoints.Add(new ApiEndpointInfo()
                    {
                        Url = urlGetDataLog
                    });
                }

                foreach (var item in apiEndpoints)
                {
                    try
                    {
                        Task.Run(async () =>
                        {
                            await ProcessStream(item.Url, token);
                        });
                    }
                    catch (Exception ex)
                    {
                        new IboxLog($"ProcessStream: {ex.Message}", "AppLogs", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"GetDataLog: {ex.Message}", "AppLogs", "Error");
            }
        }

        private async Task ProcessStream(string url, CancellationToken token)
        {
            new IboxLog($"Start ProcessStream {url}", "AppLogs", "Info");

            var pathMappings = new Dictionary<string, string>
                {
                    { "WF", DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogWF) },
                    { "SQL", DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogQuerySQL) },
                    { "API", DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogAPI) }
                };

            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                    response.EnsureSuccessStatusCode();

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        if (IBGlobalConfig.IsRunLog != "1")
                        {
                            await Task.Delay(30000, token);
                            continue;
                        }

                        token.ThrowIfCancellationRequested();
                        string? line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("*End*")) continue;

                        int separatorIndex = line.IndexOf("**");
                        if (separatorIndex == -1) continue;


                        string type = line[..separatorIndex];
                        string content = line[(separatorIndex + 2)..].Trim().Replace("*End*", "");

                        if (string.IsNullOrEmpty(content)) continue;

                        if (pathMappings.TryGetValue(type, out string? path))
                        {
                            _commonData.SaveFileLogCreateFile(content, path);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    new IboxLog($"Process for {url} was canceled.", "AppLogs", "Info");
                    break;
                }
                catch (Exception ex)
                {
                    new IboxLog($"Error from {url}: {ex.Message}, reconnecting...", "AppLogs", "Info");
                    await Task.Delay(1000, token);
                }
            }
        }


        public class ApiEndpointInfo
        {
            public string Url { get; set; } = "";
            public string TenantId { get; set; } = "";
        }
    }
}