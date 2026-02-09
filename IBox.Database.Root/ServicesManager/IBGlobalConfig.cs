using IBox.Common;
using IBox.Common.ConsistentHashing;
using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root.Tables;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IBox.Database.Root
{
    public class IBGlobalConfig : IIBGlobalConfig
    {
        public IBContext<RootContext> rootContext;
        public IRestAPI restAPI;
        private readonly Common.Objects.IConfiguration _configuration;
        public IBGlobalConfig(IBContext<RootContext> rootContext, IRestAPI restAPI, Common.Objects.IConfiguration configuration)
        {
            this.rootContext = rootContext;
            this.restAPI = restAPI;
            _configuration = configuration;
        }

        public static List<C_ConfigRoot> ConfigRoot { get; private set; } = new List<C_ConfigRoot>();
        public static List<S_Service> Services { get; private set; } = new List<S_Service>();
        public static List<T_Tenant> Tenants { get; private set; } = new List<T_Tenant>();
        public static List<string> RootServersIBox { get; private set; } = new List<string>();
        public static List<string> ServersIBox { get; private set; } = new List<string>();
        public static List<string> RestServiceIBox { get; private set; } = new List<string>();
        public static List<string> RestServiceIBoxGetDataLog { get; set; } = new List<string>();
        public static List<string> LogServiceIBox { get; private set; } = new List<string>();
        public static List<string> ScheduleServiceIBox { get; private set; } = new List<string>();
        public static List<string> ScheduleServiceIBoxGetDataLog { get; set; } = new List<string>();
        public static List<string> StoreService { get; private set; } = new List<string>();
        public static List<string> ChatBotServiceIBox { get; private set; } = new List<string>();
        public static string ServiceName { get; private set; } = string.Empty;
        public static string IsRunLog { get; set; } = string.Empty;
        public static string ThisSite { get; set; } = string.Empty;

        /// <summary>
        /// Tự động load thông tin service và config
        /// </summary>
        public void OnLoad(string serviceName = "")
        {
            try
            {
                //var ipAddress = GetLocalIPv4(NetworkInterfaceType.Wireless80211);
                var ipAddress = GetLocalIPv4(NetworkInterfaceType.Ethernet);
                //var ipAddress = "localhost";
                if (!string.IsNullOrEmpty(serviceName))
                {
                    TypeService typeService = new TypeService();
                    switch (serviceName)
                    {
                        case "ChatBotService":
                            typeService = TypeService.ChatBot;
                            break;
                        case "LogService":
                            typeService = TypeService.LogService;
                            break;
                        case "RestService":
                            typeService = TypeService.RestService;
                            break;
                        case "RootBEService":
                            typeService = TypeService.RootBE;
                            break;
                        case "ScheduleService":
                            typeService = TypeService.ScheduleService;
                            break;
                        case "StoreService":
                            typeService = TypeService.StoreService;
                            break;
                        case "ClientBEService":
                            typeService = TypeService.ClientBE;
                            break;
                    }

                    var s_Services = this.rootContext.Context.S_Services.FirstOrDefault(ptr => ptr.Site == ipAddress && ptr.TypeService == typeService);
                    if (s_Services == null)
                    {
                        this.rootContext.Context.S_Services.Add(new S_Service()
                        {
                            Site = ipAddress,
                            CreatedDate = DateTime.Now,
                            Id = Guid.NewGuid().ToString(),
                            IsDelete = false,
                            IsOnline = true,
                            Level = "1",
                            ModificationDate = DateTime.Now,
                            Name = serviceName,
                            Port = _configuration.Config.Value.Port,
                            SubDomain = _configuration.Config.Value.SubDomain,
                            TypeProtocol = (TypeProtocol)_configuration.Config.Value.TypeProtocol,
                            TypeService = typeService,
                        });

                        this.rootContext.Context.SaveChanges();
                    }
                }

                IBGlobalConfig.Services = this.rootContext.Context.S_Services.Where(ptr => ptr.IsDelete != true).ToList();
                IBGlobalConfig.Tenants = this.rootContext.Context.T_Tenants.Where(ptr => ptr.IsDelete != true).ToList();
                IBGlobalConfig.ThisSite = ipAddress;

                LoadService();

                new IboxLog($"Load Service: {serviceName}", "AppLogs", "Info");

                if (!string.IsNullOrEmpty(serviceName))
                {
                    IBGlobalConfig.ServiceName = serviceName;
                }

                string logStatusFilePath = Path.Combine(Directory.GetCurrentDirectory(), "status.log");
                if (IBGlobalConfig.ServiceName == "LogService")
                {
                    if (File.Exists(logStatusFilePath))
                    {
                        IBGlobalConfig.IsRunLog = File.ReadAllText(logStatusFilePath);
                    }
                    else
                    {
                        File.WriteAllText(logStatusFilePath, "1");
                        IBGlobalConfig.IsRunLog = "1";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Can not load service list config from root or system has just init. Error: {ex.Message}", ex);
            }
        }
        public void LoadService()
        {
            try
            {
                IBGlobalConfig.RootServersIBox = IBGlobalConfig.Services
                   .Where(ptr => ptr.TypeService == Tables.TypeService.RootBE)
                   .Select(ptr => this.buildURL(ptr))
                   .ToList();
                IBGlobalConfig.ServersIBox = IBGlobalConfig.Services
                    .Where(ptr => ptr.TypeService == Tables.TypeService.ClientBE)
                    .Select(ptr => this.buildURL(ptr))
                    .ToList();
                IBGlobalConfig.RestServiceIBox = IBGlobalConfig.Services
                    .Where(ptr => ptr.TypeService == Tables.TypeService.RestService)
                    .Select(ptr => this.buildURL(ptr))
                    .ToList();

                IBGlobalConfig.LogServiceIBox = IBGlobalConfig.Services
                    .Where(ptr => ptr.TypeService == Tables.TypeService.LogService)
                    .Select(ptr => this.buildURL(ptr))
                    .OrderBy(ptr => ptr)
                    .ToList();
                IBGlobalConfig.ScheduleServiceIBox = IBGlobalConfig.Services
                    .Where(ptr => ptr.TypeService == Tables.TypeService.ScheduleService)
                    .Select(ptr => this.buildURL(ptr))
                    .ToList();
                IBGlobalConfig.StoreService = IBGlobalConfig.Services
                    .Where(ptr => ptr.TypeService == Tables.TypeService.StoreService)
                    .Select(ptr => this.buildURL(ptr))
                    .ToList();

                IBGlobalConfig.ChatBotServiceIBox = IBGlobalConfig.Services
                   .Where(x => x.TypeService == Tables.TypeService.ChatBot)
                   .Select(x => this.buildURL(x)).ToList();

                ConsistentHashing.AddListServerVirtual(IBGlobalConfig.LogServiceIBox);
            }
            catch (Exception ex)
            {
                Log.Error($"LoadService: {ex.Message}");
            }
        }

        public string GetLocalIPv4(NetworkInterfaceType _type)
        {
            try
            {
                foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                    {
                        var ip1 = item.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork
                                              && !IPAddress.IsLoopback(ip.Address));
                        if (ip1 != null)
                        {
                            return ip1.Address.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"GetLocalIPv4: {ex.Message}");
            }

            return "";
        }
        public void LoadConfigRoot()
        {
            try
            {
                IBGlobalConfig.ConfigRoot = this.rootContext.Context.C_ConfigRoot.Where(ptr => ptr.IsDelete != true).ToList();
                this.rootContext.Context.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error($"Can not load ConfigRoot: {ex.Message}");
            }
        }

        public void MailAlertRunService(string ServiceName)
        {
            try
            {
                Task.Run(() =>
                {
                    Thread.Sleep(4000);
                    this.restAPI.SendMailAlert(LogServiceIBox, new
                    {
                        Id = "MailAlertROOT",
                        MailTitle = $"[Warning] Start Service",
                        MailBody = $"Dear Team, <BR><BR>Service {ServiceName} Running on Server {IBGlobalConfig.ThisSite}",
                        typeWarning = TypeWarning.ServiceRunning
                    });
                });
            }
            catch (Exception ex)
            {
                Log.Error($"MailAlertRunService. Error: {ex.Message}", ex);
            }
        }

        private string buildURL(S_Service s_Service)
        {
            string host = $"{s_Service.TypeProtocol}://{s_Service.Site ?? string.Empty}";
            if (!string.IsNullOrEmpty(s_Service.Port))
            {
                host = $"{host}:{s_Service.Port}";
            }

            if (!string.IsNullOrEmpty(s_Service.SubDomain))
            {
                host = $"{host}/{s_Service.SubDomain}";
            }

            return host;
        }

        /// <summary>
        /// Yêu cầu tất cả các service thực hiện reload lại cấu hình vào cache của ứng dụng
        /// </summary>
        /// <param name="request"></param>
        public void SendRequestReload(HttpRequest request)
        {
            try
            {
                var Authorize = request.Headers["Authorization"];
                var allService = this.rootContext.Context.S_Services.Where(ptr => ptr.IsDelete != true).ToList();
                this.rootContext.Context.Dispose();
                allService.ForEach(e =>
                {
                    Task.Run(() =>
                        {
                            try
                            {
                                var rest = $"{buildURL(e)}/api/ReloadOK";
                                var handler = new HttpClientHandler();
                                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                                HttpClient httpClient = new HttpClient(handler);
                                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, rest);
                                requestMessage.Headers.Add("Authorization", Authorize.ToString());
                                HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Can't synch config to Server", ex.Message, buildURL(e));
                            }
                        });
                });
            }
            catch
            {
            }
        }
        /// <summary>
        /// Yêu cầu tất cả các config cung cấp config đang được triển khai
        /// </summary>
        /// <param name="requestContext"></param>
        public List<IBGlobalConfigList> GetAllConfiguration(HttpRequest requestContext)
        {
            try
            {
                List<IBGlobalConfigList> dynamics = new List<IBGlobalConfigList>();
                var Authorize = requestContext.Headers["Authorization"];
                IBGlobalConfig.Services.ToList().ForEach(e =>
                {
                    try
                    {
                        var rest = $"{buildURL(e)}/api/GetConfiguration";
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, rest);
                        requestMessage.Headers.Add("Authorization", Authorize.ToString());
                        HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                        var Result = httpResponse.Content.ReadAsStringAsync().Result;
                        var obj = JsonConvert.DeserializeObject<ResponseForm<IBGlobalConfigList>>(Result);
                        if (obj != null)
                        {

                            dynamics.Add(obj.Data);

                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Can't synch config to Server", ex.Message, buildURL(e));
                    }
                });

                return dynamics;
            }
            catch
            {
                return new List<IBGlobalConfigList>();
            }
        }
    }

    public class IBGlobalConfigList
    {
        private string site = string.Empty;
        private List<S_Service> services = new List<S_Service>();

        public string Site { get => site; set => site = value; }
        public List<S_Service> Services { get => services; set => services = value; }
    }
}