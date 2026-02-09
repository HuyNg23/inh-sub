using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant.Tables;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Net.Http.Headers;

namespace IBox.Database.Tenant
{
    public class IBGlobalTenantConfig : IIBGlobalTenantConfig
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public IBGlobalTenantConfig(IConfiguration configuration, IEncryption encryption)
        {
            _configuration = configuration;
            _encryption = encryption;
        }

        public static List<CB_ConfigChat> ConfigChatBots { get; private set; } = new List<CB_ConfigChat>();

        /// <summary>
        /// Tự động load thông tin service và config
        /// </summary>
        public void OnLoadConfigChatBot()
        {
            try
            {
                IBGlobalTenantConfig.ConfigChatBots = new List<CB_ConfigChat>();
                if (IBGlobalConfig.Tenants.Any())
                {
                    foreach (var tenantId in IBGlobalConfig.Tenants)
                    {
                        try
                        {
                            var context = new TenantContext(this._configuration, this._encryption, new RootContext(this._configuration, this._encryption));
                            var tenantContext = context.GetTenantContext(tenantId.Id).Context;
                            var configChat = tenantContext.CB_ConfigChats.Where(ptr => ptr.IsDelete != true && ptr.TypeChat == TypeChat.ChatBot).ToList();
                            if (configChat.Any())
                            {
                                foreach (var rowConfig in configChat)
                                {
                                    IBGlobalTenantConfig.ConfigChatBots.Add(new CB_ConfigChat()
                                    {
                                        EndChatTime = rowConfig.EndChatTime,
                                        Ev_EndChat = rowConfig.Ev_EndChat,
                                        Ev_Predict = rowConfig.Ev_Predict,
                                        Ev_Tag = rowConfig.Ev_Tag,
                                        Ev_User_Support = rowConfig.Ev_User_Support,
                                        FileDay = rowConfig.FileDay,
                                        IC_Token = rowConfig.IC_Token,
                                        Id = rowConfig.Id,
                                        IdBOT = rowConfig.IdBOT,
                                        ListToken = rowConfig.ListToken,
                                        PassChatBot = rowConfig.PassChatBot,
                                        PathBackUp = rowConfig.PathBackUp,
                                        PathRestore = rowConfig.PathRestore,
                                        CreatedDate = rowConfig.CreatedDate,
                                        IsDelete = rowConfig.IsDelete,
                                        ModificationDate = DateTime.Now,
                                        Retry = rowConfig.Retry,
                                        tenant_id = rowConfig.tenant_id,
                                        TokenBOTSendChatBot = rowConfig.TokenBOTSendChatBot,
                                        TypeChat = rowConfig.TypeChat,
                                        UrlShowChatIBox = rowConfig.UrlShowChatIBox,
                                        Url_Api_BOT_DisableBot = rowConfig.Url_Api_BOT_DisableBot,
                                        Url_Api_BOT_EnableBot = rowConfig.Url_Api_BOT_EnableBot,
                                        Url_Api_BOT_SendMessage = rowConfig.Url_Api_BOT_SendMessage,
                                        Url_Api_BOT_UploadFile = rowConfig.Url_Api_BOT_UploadFile,
                                        Url_IC = rowConfig.Url_IC,
                                        UserChatBot = rowConfig.UserChatBot,
                                        WF_CustomerIdentification = rowConfig.WF_CustomerIdentification,
                                        WF_InteractionCRM = rowConfig.WF_InteractionCRM,
                                        WF_PutAllChatIC = rowConfig.WF_PutAllChatIC,
                                        WF_SendChatIC = rowConfig.WF_SendChatIC,
                                        WF_UpdateIC_Interaction = rowConfig.WF_UpdateIC_Interaction,
                                        WF_UpdateInteractionCRM = rowConfig.WF_UpdateInteractionCRM
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Load config Chat: {ex.Message} \n tenantId: {JsonConvert.SerializeObject(tenantId)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Can not load service list config from root or system has just init. Error: {ex.Message}", ex);
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
        private List<CB_ConfigChat>? configChat;

        public string Site { get => site; set => site = value; }
        public List<S_Service> Services { get => services; set => services = value; }

        public List<CB_ConfigChat> ConfigChat { get => configChat; set => configChat = value; }

    }
}