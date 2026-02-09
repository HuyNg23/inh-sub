using IBox.Common.Objects;
using IBox.Common.ServiceIB;
using IBox.Database.Root;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace IBox.DLEx.Implementation
{
    public partial class ExecuteWF
    {
        public List<WFDefine> ViewWFDeploy(HttpRequest requestContext, ViewDeployServiceType viewDeployServiceType)
        {
            try
            {
                List<WFDefine> wFDefines = new List<WFDefine>();
                var Authorize = requestContext.Headers["Authorization"];
                var services = new List<string>();
                switch (viewDeployServiceType)
                {
                    case ViewDeployServiceType.Rest:
                        services = IBGlobalConfig.RestServiceIBox;
                        break;

                    case ViewDeployServiceType.Schedule:
                        services = IBGlobalConfig.ScheduleServiceIBox;
                        break;

                    case ViewDeployServiceType.Log:
                        services = IBGlobalConfig.LogServiceIBox;
                        break;

                    case ViewDeployServiceType.ChatBot:
                    default:
                        services = IBGlobalConfig.ChatBotServiceIBox;
                        break;
                }
                services.ToList().ForEach(ip =>
                {
                    try
                    {
                        var rest = string.Format(@"{0}{1}", ip, UrlServiceIBConfig.UrlGetAllWFDeploy);
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                        HttpClient httpClient = new HttpClient(handler);
                        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);

                        requestMessage.Content = new StringContent(@"{}", Encoding.UTF8, "Application/json");
                        requestMessage.Headers.Add("Authorization", Authorize.ToString());
                        HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            string result = httpResponse.Content.ReadAsStringAsync().Result;
                            if (!string.IsNullOrEmpty(result))
                            {
                                ResponseForm<List<WFDefine>>? wfs = JsonConvert.DeserializeObject<ResponseForm<List<WFDefine>>>(result);
                                if (wfs != null && wfs.Data != null)
                                {
                                    wfs.Data.ForEach(ptr => ptr.Source = ip);
                                    wFDefines.AddRange(wfs.Data);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, ex.Message);
                    }
                });

                return wFDefines;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<WFDefine> ViewWFDeploy()
        {
            return _workflowControl.AllWFDeploy();
        }

        public void Deploy(string wfId, HttpRequest requestContext, ViewDeployServiceType viewDeployServiceType)
        {
            try
            {
                var services = new List<string>();
                switch (viewDeployServiceType)
                {
                    case ViewDeployServiceType.Rest:
                        services = IBGlobalConfig.RestServiceIBox;
                        break;

                    case ViewDeployServiceType.Schedule:
                        services = IBGlobalConfig.ScheduleServiceIBox;
                        break;

                    case ViewDeployServiceType.Log:
                        services = IBGlobalConfig.LogServiceIBox;
                        break;

                    case ViewDeployServiceType.ChatBot:
                    default:
                        services = IBGlobalConfig.ChatBotServiceIBox;
                        break;
                }
                var Authorize = requestContext.Headers["Authorization"];

                var wF_Defines = this.tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == wfId);

                var tenantID = requestContext.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                if (wF_Defines == null)
                {
                    throw new IboxLog($"Can not found workflow id {wfId}", tenantID);
                }

                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantID,
                    MailTitle = "[Warning] API Workflow được triển khai",
                    MailBody = $"{wF_Defines.Name}",
                    WFID = wfId,
                    typeWarning = TypeWarning.WorkflowDeployment
                });

                services.ToList().ForEach(ip =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var rest = string.Format(@"{0}{1}/{2}", ip, UrlServiceIBConfig.UrlDeployWF, wfId);
                            var handler = new HttpClientHandler();
                            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                            HttpClient httpClient = new HttpClient(handler);
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);

                            requestMessage.Content = new StringContent(@"{}", Encoding.UTF8, "Application/json");
                            requestMessage.Headers.Add("Authorization", Authorize.ToString());
                            HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                            Log.Information("Deloy WF to server ", ip);
                            Log.Information(httpResponse.Content.ReadAsStringAsync().Result);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ex.Message);
                        }
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Deploy(string wfid, string tenantId)
        {
            this._workflowControl.Deploy(wfid, tenantId);
        }

        public void Recovery(string wfId, HttpRequest requestContext, ViewDeployServiceType viewDeployServiceType)
        {
            try
            {
                var Authorize = requestContext.Headers["Authorization"];


                var wF_Defines = this.tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == wfId);

                var tenantID = requestContext.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

                if (wF_Defines == null)
                {
                    throw new IboxLog($"Can not found workflow id {wfId}", tenantID);
                }


                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantID,
                    MailTitle = "[Warning] API được thu hồi",
                    MailBody = $"{wF_Defines.Name}",
                    WFID = wfId,
                    typeWarning = TypeWarning.APIRecall
                });

                var services = new List<string>();
                switch (viewDeployServiceType)
                {
                    case ViewDeployServiceType.Rest:
                        services = IBGlobalConfig.RestServiceIBox;
                        break;

                    case ViewDeployServiceType.Schedule:
                        services = IBGlobalConfig.ScheduleServiceIBox;
                        break;

                    case ViewDeployServiceType.Log:
                        services = IBGlobalConfig.LogServiceIBox;
                        break;

                    case ViewDeployServiceType.ChatBot:
                    default:
                        services = IBGlobalConfig.ChatBotServiceIBox;
                        break;
                }

                services.ToList().ForEach(ip =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var rest = string.Format(@"{0}{1}/{2}", ip, UrlServiceIBConfig.UrlRecoveryWF, wfId);
                            var handler = new HttpClientHandler();
                            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                            HttpClient httpClient = new HttpClient(handler);
                            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, rest);
                            requestMessage.Content = new StringContent(@"{}", Encoding.UTF8, "Application/json");
                            requestMessage.Headers.Add("Authorization", Authorize.ToString());
                            HttpResponseMessage httpResponse = httpClient.SendAsync(requestMessage).Result;
                            Log.Information("Recovery WF to server ", ip);
                            Log.Information(httpResponse.Content.ReadAsStringAsync().Result);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, ex.Message);
                        }
                    });
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Recovery(string wfid)
        {
            this._workflowControl.Recovery(wfid);
        }
    }

    public enum ViewDeployServiceType
    {
        Rest,
        Schedule,
        ChatBot,
        Log
    }
}