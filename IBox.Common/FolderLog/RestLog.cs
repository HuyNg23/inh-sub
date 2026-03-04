using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.Model;
using IBox.Common.Objects;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;

namespace IBox.Common.FolderLog
{
    public class RestLog : IRestLog
    {
        private static ConcurrentDictionary<string, List<string>> _logBuffer = new();
        private readonly int _maxBatchSize = 100;

        private static readonly Channel<KeyValuePair<string, string>> _logChannel =
            Channel.CreateBounded<KeyValuePair<string, string>>(new BoundedChannelOptions(5000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        private readonly IServiceProvider _serviceProvider;

        public RestLog(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private ICommonData CommonData => _serviceProvider.GetRequiredService<ICommonData>();

        public void WriteFileLogWF(string serviceName, object obj)
        {
            try
            {
                string? body = JsonConvert.SerializeObject(obj);
                if (string.IsNullOrEmpty(body))
                {
                    return;
                }

                var _commonData = CommonData;
                var wfModel = JsonConvert.DeserializeObject<WFModel>(body);
                if (wfModel == null)
                {
                    return;
                }
                switch (serviceName)
                {
                    case "ClientBEService":
                        return;

                    case "LogService":
                        string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogWF);

                        if (_commonData != null)
                        {
                            _commonData.SaveFileLogCreateFile(JsonConvert.SerializeObject(wfModel), path);
                        }
                        return;

                    default:
                        AddLogCacheAsync("WF", JsonConvert.SerializeObject(wfModel));
                        break;
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"WriteFileLogWFAsync: {ex.Message} \n content: {obj}", "AppLogs", "Error");
            }
        }

        public void WriteFileLogAPI(string serviceName, object obj)
        {
            try
            {
                string? body = JsonConvert.SerializeObject(obj);
                if (string.IsNullOrEmpty(body))
                {
                    return;
                }

                var apiThirdPartyModel = JsonConvert.DeserializeObject<APIThirdPartyModel>(body);

                if (apiThirdPartyModel == null)
                {
                    return;
                }

                var _commonData = CommonData;
                switch (serviceName)
                {
                    case "ClientBEService":
                        return;
                    case "LogService":
                        string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogAPI);

                        if (_commonData != null)
                        {
                            _commonData.SaveFileLogCreateFile(JsonConvert.SerializeObject(apiThirdPartyModel), path);
                        }
                        return;
                    default:
                        AddLogCacheAsync($"API", JsonConvert.SerializeObject(apiThirdPartyModel));
                        break;
                }

                if (serviceName.Contains("ClientBEService"))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"WriteFileLogAPI: {ex.Message} \n content: {obj.ToString()}", "AppLogs", "Error");
            }
        }

        public void WriteFileLogSQL(string serviceName, object obj)
        {
            try
            {
                string? body = JsonConvert.SerializeObject(obj);
                if (string.IsNullOrEmpty(body))
                {
                    return;
                }

                var executeQuerySQL = JsonConvert.DeserializeObject<ExecuteQuerySQLModel>(body);
                if (executeQuerySQL == null)
                {
                    return;
                }

                var _commonData = CommonData;
                switch (serviceName)
                {
                    case "ClientBEService":
                        return;
                    case "LogService":
                        string path = DataPath.CombineWithRuntimeRoot(DataPath.TemporaryFolderLogQuerySQL);

                        if (_commonData != null)
                        {
                            _commonData.SaveFileLogCreateFile(JsonConvert.SerializeObject(executeQuerySQL), path);
                        }
                        return;
                    default:
                        AddLogCacheAsync($"SQL", JsonConvert.SerializeObject(executeQuerySQL));
                        break;
                }
            }
            catch (Exception ex)
            {
                new IboxLog($"WriteFileLogSQL: {ex.Message} \n content: {obj.ToString()}", "AppLogs", "Error");
            }
        }

        public List<LogInfo> ListLog(string location)
        {
            string parth = Directory.GetCurrentDirectory() + $"{Path.DirectorySeparatorChar}Logs";
            DirectoryInfo d = new DirectoryInfo(parth);

            FileInfo[] Files = d.GetFiles();
            List<LogInfo> logInfos = new List<LogInfo>();

            Files.OrderByDescending(ptr => ptr.CreationTime).ToList().ForEach(f =>
                    {
                        logInfos.Add(new LogInfo()
                        {
                            Location = location,
                            Name = f.Name,
                            Size = f.Length / 1024,
                            CreatedDate = f.CreationTime,
                            ModifiedDate = f.LastWriteTime,
                        });
                    });

            return logInfos;
        }

        #region Cache
        public async Task AddLogCacheAsync(string type, string log)
        {
            await _logChannel.Writer.WriteAsync(new KeyValuePair<string, string>(type, log));
        }
        public async Task<KeyValuePair<string, string>?> TakeLastLogAsync(TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                return await _logChannel.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }


        public async Task StreamDataLogCache(HttpResponse Response, HttpContext httpContext)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            await using var streamWriter = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);

            try
            {
                while (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(100, httpContext.RequestAborted);

                        var batchLogs = new List<KeyValuePair<string, string>>();

                        for (int i = 0; i < 100; i++)
                        {
                            var logEntry = await TakeLastLogAsync(TimeSpan.FromMilliseconds(2000));

                            if (logEntry.HasValue)
                            {
                                batchLogs.Add(logEntry.Value);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (!batchLogs.Any())
                        {
                            continue;
                        }

                        // Gom nhóm log theo loại
                        var groupedLogs = batchLogs.GroupBy(log => log.Key);
                        foreach (var group in groupedLogs)
                        {
                            string type = group.Key;
                            List<string> logs = group.Select(log => log.Value).ToList();

                            _logBuffer.AddOrUpdate(type,
                                _ => new List<string>(logs),
                                (_, oldList) =>
                                {
                                    oldList.AddRange(logs);
                                    return oldList;
                                });
                        }

                        foreach (var type in _logBuffer.Keys.ToList())
                        {
                            if (_logBuffer.TryGetValue(type, out var logs))
                            {
                                string data = string.Join(",", logs);

                                // Kiểm tra response có còn mở hay không
                                if (!Response.HasStarted || !httpContext.RequestAborted.IsCancellationRequested)
                                {
                                    await streamWriter.WriteLineAsync($"{type}**{data}*End*");
                                    await streamWriter.FlushAsync();
                                }

                                _logBuffer.TryRemove(type, out _);
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not TaskCanceledException)
                    {
                        new IboxLog($"StreamDataLogCache error: {ex}", "AppLogs", "Error");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                new IboxLog("StreamDataLogCache: Request was canceled.", "AppLogs", "Error");
            }
            catch (Exception ex)
            {
                new IboxLog($"StreamDataLogCache unexpected error: {ex}", "AppLogs", "Error");
            }
        }

        #endregion
    }
}