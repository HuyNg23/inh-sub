using IBox.Common.Objects;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog.Context;
using System.Net;
using System.Text;

namespace IBox.Common.TCP
{
    public class RestApi : IBoxDisposable, IRestAPI
    {
        private readonly Objects.IConfiguration _configuration;

        public RestApi(Objects.IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private HttpClient initClient()
        {
            return new HttpClient();
        }

        public RestAPIResponse Send(RestAPIRequest req, string tenantId)
        {
            try
            {
                RestAPIRequest rest = req;
                HttpClient httpClient = initClient();
                HttpResponseMessage httpResponse;
                HttpRequestMessage requestMessage = SendHandle(req, tenantId);
                List<RestAPIHeader> headers = rest.Headers ?? new List<RestAPIHeader>();
                var body = SanitizeJson(rest.Body ?? "");
                
                if (headers.Any())
                {
                    var contentType = headers.FirstOrDefault(ptr => ptr?.Label?.ToUpper() == "Content-type".ToUpper());

                    if (requestMessage.Method != HttpMethod.Get)
                    {
                        if (contentType != null)
                        {
                            requestMessage.Content = new StringContent(body, Encoding.UTF8, contentType.Value);
                        }
                        else
                        {
                            requestMessage.Content = new StringContent(body, Encoding.UTF8);
                        }
                    }
                }
                else
                {
                    requestMessage.Content = new StringContent(body, Encoding.UTF8);
                }

                foreach (RestAPIHeader header in headers)
                {
                    if (header == null)
                    {
                        throw new IboxLog("header is null", tenantId);
                    }

                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Label.ToString(), header.Value.ToString());
                }

                httpResponse = httpClient.Send(requestMessage);

                return new RestAPIResponse()
                {
                    Request = JsonConvert.SerializeObject(new
                    {
                        StatusCode = httpResponse.StatusCode,
                        RequestMessage = httpResponse.RequestMessage
                    }),
                    Result = httpResponse.Content.ReadAsStringAsync().Result
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public RestAPIResponse SendChatBot(RestAPIRequest req, string tenantId = "AppLogs")
        {
            try
            {
                RestAPIRequest rest = req;
                HttpClient httpClient = initClient();
                HttpResponseMessage httpResponse;
                HttpRequestMessage requestMessage = SendHandle(req, tenantId);

                List<RestAPIHeader> headers = rest.Headers ?? new List<RestAPIHeader>();

                if (headers.Any())
                {
                    var contentType = headers.FirstOrDefault(ptr => (ptr.Label != null) && ptr.Label.ToUpper() == "Content-type".ToUpper());

                    if (requestMessage.Method != HttpMethod.Get)
                    {
                        string body = rest.Body ?? "";

                        if (contentType != null)
                        {
                            requestMessage.Content = new StringContent(rest.Body, Encoding.UTF8, contentType.Value);
                        }
                        else
                        {
                            requestMessage.Content = new StringContent(rest.Body, Encoding.UTF8);
                        }
                    }
                }
                else
                {
                    requestMessage.Content = new StringContent(rest.Body ?? "", Encoding.UTF8);
                }

                foreach (RestAPIHeader? header in headers)
                {
                    if (header?.Label != null && header?.Value != null)
                    {
                        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Label.ToString(), header.Value.ToString());
                    }
                }

                httpResponse = httpClient.Send(requestMessage);

                return new RestAPIResponse()
                {
                    Request = JsonConvert.SerializeObject(new
                    {
                        StatusCode = httpResponse.StatusCode,
                        RequestMessage = httpResponse.RequestMessage
                    }),
                    Result = httpResponse.Content.ReadAsStringAsync().Result
                };
            }
            catch (Exception ex)
            {
                new IboxLog($"SendChatBot: {ex.Message} \n req: {JsonConvert.SerializeObject(req)}", tenantId);
                throw;
            }
        }

        public RestAPIResponse Send(RestAPIRequestWFConfig req, string tenantId)
        {
            try
            {
                var rest = req;
                HttpClient httpClient = initClient();
                HttpRequestMessage requestMessage = SendHandle(req, tenantId);
                var body = SanitizeJson(rest.Body ?? "");

                if (rest.Headers.IsNullOrEmpty())
                {
                    throw new IboxLog("Headers is required", tenantId);
                }

                List<RestAPIHeader>? headers = rest.Headers;

                if (headers == null || headers.Count == 0)
                {
                    throw new IboxLog("Can not convert or header is empty", tenantId);
                }

                var contentType = headers.FirstOrDefault(ptr => ptr?.Label?.ToUpper() == "Content-type".ToUpper());

                if (requestMessage.Method != HttpMethod.Get)
                {
                    if (contentType != null)
                    {
                        requestMessage.Content = new StringContent(body, Encoding.UTF8, contentType.Value);
                    }
                    else
                    {
                        requestMessage.Content = new StringContent(body, Encoding.UTF8);
                    }
                }

                foreach (dynamic? header in headers)
                {
                    if (header == null)
                    {
                        throw new IboxLog("header is null", tenantId);
                    }

                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Label.ToString(), header.Value.ToString());
                }

                httpClient.Timeout = TimeSpan.FromSeconds(req.Timeout);

                HttpResponseMessage httpResponse = httpClient.Send(requestMessage);
                return new RestAPIResponse()
                {
                    HttpResponse = httpResponse,
                    Result = httpResponse.Content.ReadAsStringAsync().Result
                };
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SendAsync(RestAPIRequestWFConfig req, string tenantId, Action<string, HttpResponseMessage>? functionCallbackSuccess = null, Action<string, HttpResponseMessage>? functionCallbackError = null, Action<string, string>? functionCallbackErrorTask = null)
        {
            try
            {
                var rest = req;
                HttpClient httpClient = initClient();
                HttpRequestMessage requestMessage = SendHandle(req, tenantId);
                var body = SanitizeJson(rest.Body ?? "");

                if (rest.Headers.IsNullOrEmpty())
                {
                    throw new IboxLog("Headers is required", tenantId);
                }

                List<RestAPIHeader>? headers = rest.Headers;

                if (headers == null || headers.Count == 0)
                {
                    throw new IboxLog("Can not convert or header is empty", tenantId);
                }

                var contentType = headers.FirstOrDefault(ptr => ptr?.Label?.ToUpper() == "Content-type".ToUpper());

                if (requestMessage.Method != HttpMethod.Get)
                {

                    if (contentType != null)
                    {
                        requestMessage.Content = new StringContent(body, Encoding.UTF8, contentType.Value);
                    }
                    else
                    {
                        requestMessage.Content = new StringContent(body, Encoding.UTF8);
                    }
                }

                foreach (dynamic? header in headers)
                {
                    if (header == null)
                    {
                        throw new IboxLog("header is null", tenantId);
                    }

                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Label.ToString(), header.Value.ToString());
                }

                httpClient.Timeout = TimeSpan.FromSeconds(req.Timeout);

                if (functionCallbackSuccess == null || functionCallbackError == null)
                {
                    var httpResponse = httpClient.SendAsync(requestMessage);
                }
                else
                {
                    var httpResponse = httpClient.SendAsync(requestMessage).ContinueWith(async task =>
                    {
                        string jsonResponse = string.Empty;
                        if (task.IsCompletedSuccessfully)
                        {
                            HttpResponseMessage response = task.Result;

                            if (response.IsSuccessStatusCode)
                            {
                                functionCallbackSuccess(rest.wfExecuteSuccess, response);
                            }
                            else
                            {
                                functionCallbackError(rest.wfExecuteError, response);
                                new IboxLog($"Task Call Back Response StatusCode: {response.StatusCode} \n Data Response: {await response.Content.ReadAsStringAsync()}", tenantId, "Error");
                            }
                        }
                        else if (task.IsFaulted)
                        {
                            functionCallbackErrorTask(rest.wfExecuteError, task.Exception.Message);

                            using (LogContext.PushProperty("TenantId", tenantId))
                            {
                                new IboxLog($"Task CallBack: {task.Exception.Message}", tenantId, "Error");
                            }
                        }
                    });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private HttpRequestMessage SendHandle(RestAPIRequest rest, string tenantId)
        {
            try
            {
                switch (rest.Method)
                {
                    case "GET":
                        return new HttpRequestMessage(HttpMethod.Get, rest.Url);

                    case "POST":
                        if (string.IsNullOrEmpty(rest.Body))
                        {
                            throw new IboxLog("Body is required", tenantId);
                        }
                        return new HttpRequestMessage(HttpMethod.Post, rest.Url);

                    case "PUT":
                        if (string.IsNullOrEmpty(rest.Body))
                        {
                            throw new IboxLog("Body is required", tenantId);
                        }
                        return new HttpRequestMessage(HttpMethod.Put, rest.Url);

                    case "DELETE":
                        return new HttpRequestMessage(HttpMethod.Delete, rest.Url);

                    default:
                        return new HttpRequestMessage(HttpMethod.Get, rest.Url);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        public void SendMailAlert(List<string> LogServiceIBox, object obj)
        {
            try
            {
                //List<RestAPIHeader>? headers = new List<RestAPIHeader>();
                //RestAPIHeader restAPI = new RestAPIHeader
                //{
                //    Label = "Content-Type",
                //    Value = "application/json"
                //};
                //headers.Add(restAPI);
                //foreach (var ip in LogServiceIBox)
                //{
                //    try
                //    {
                //        string urlLogService = string.Format(@"{0}{1}",
                //                ip,
                //                UrlServiceIBConfig.UrlAPISendMailServer);

                //        this.SendAsync(new RestAPIRequestWFConfig
                //        {
                //            Url = urlLogService,
                //            Method = "POST",
                //            Body = JsonConvert.SerializeObject(obj),
                //            Headers = headers
                //        });
                //        break;
                //    }
                //    catch (Exception ex)
                //    {
                //        Log.Error(ex, "CallAPI SendAPIMailServer Send", ex.Message);
                //        continue;
                //    }
                //}
            }
            catch (Exception ex)
            {
                new IboxLog("CallAPI SendAPIMailServer Send " + ex.Message, "AppLogs", "Error");
            }
        }

        private string SanitizeJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj);
            }
            catch
            {
                return json;
            }
        }
    }

    public class RestAPIResponse
    {
        private string? request;
        private string? result;
        private HttpResponseMessage httpResponse = new HttpResponseMessage()
        {
            StatusCode = (HttpStatusCode)0
        };

        public string? Request { get => request; set => request = value; }
        public string? Result { get => result; set => result = value; }
        public HttpResponseMessage HttpResponse { get => httpResponse; set => httpResponse = value; }
    }

    /// <summary>
    /// Model for request in client
    /// </summary>
    public class RestAPIRequest
    {
        private string? url;
        private string? method;
        private string? body;
        private List<RestAPIHeader>? headers;

        public string? Url { get => url; set => url = value; }
        public string? Method { get => method; set => method = value; }
        public string? Body { get => body; set => body = value; }
        public List<RestAPIHeader>? Headers { get => headers; set => headers = value; }
        public int Timeout { get; set; } = 30;
    }

    public enum AuthorType
    {
        None,
        Basic
    }

    public class AuthenticationRequest
    {
        private AuthorType? type;
        private string? userName;
        private string? password;
        public AuthorType? Type { get => type; set => type = value; }
        public string? UserName { get => userName; set => userName = value; }
        public string? Password { get => password; set => password = value; }
    }

    /// <summary>
    /// Model for step config
    /// </summary>
    public class RestAPIRequestWFConfig : RestAPIRequest
    {
        private AuthenticationRequest? authen;
        private List<RestAPIHeader>? headers;
        private bool executeAsync;
        private RestAPIRequestWFExceptionConfig exception = new RestAPIRequestWFExceptionConfig();
        public new List<RestAPIHeader>? Headers { get => headers; set => headers = value; }
        public AuthenticationRequest? Authen { get => authen; set => authen = value; }
        public bool ExecuteAsync { get => executeAsync; set => executeAsync = value; }
        public string wfExecuteSuccess { get; set; } = "";
        public string wfExecuteError { get; set; } = "";
        public RestAPIRequestWFExceptionConfig Exception { get => exception; set => exception = value; }
    }

    public class RestAPIRequestWFExceptionConfig
    {
        public List<RestAPIRequestWFExceptionHttpCodeResultConfig> HttpCode { get; set; } = new List<RestAPIRequestWFExceptionHttpCodeResultConfig>();
        public List<RestAPIRequestWFExceptionBodyResultConfig> BodyResponse { get; set; } = new List<RestAPIRequestWFExceptionBodyResultConfig>();
    }

    public class RestAPIRequestWFExceptionHttpCodeResultConfig
    {
        public string Key { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
    }

    public class RestAPIRequestWFExceptionBodyResultConfig
    {
        public string Key { get; set; } = Guid.NewGuid().ToString();
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for step config
    /// </summary>
    public class RestAPIHeader
    {
        private string? label;
        private string? value;

        public string? Label { get => label; set => label = value; }
        public string? Value { get => value; set => this.value = value; }
    }

    
    public class UploadFileConfig
    {
        public string Source { get; set; } = "Temp"; // "Temp" or "Folder"
        public string FolderPath { get; set; } = "";
        public string FilePattern { get; set; } = "*.*";
        public string TargetUrl { get; set; } = "";
        public string Method { get; set; } = "POST";
        public string FileFormField { get; set; } = "file";
        public Dictionary<string, string>? Headers { get; set; }

        // FormFields is now a list of detailed objects (key, label, value, type)
        public List<FormFieldConfig>? FormFields { get; set; }

        // Optional auth info
        public AuthenConfig? authen { get; set; }
    }

    // Nested model for authen (if used)
    public class AuthenConfig
    {
        public int type { get; set; } = 0; // 0: none, 1: basic, 2: bearer, etc.
        public string? userName { get; set; }
        public string? password { get; set; }
    }

    // Nested model for each form field item
    public class FormFieldConfig
    {
        public string? key { get; set; }       // unique id in config (for UI mapping)
        public string? label { get; set; }     // actual field name sent to API
        public string? value { get; set; }     // template value, e.g. [stepId.fieldName]
        public string? type { get; set; }      // "file" | "text" | (future maybe "number", etc.)
    }

    public class XRestAPIRequestWFConfig
    {
        public string? TargetUrl { get; set; }
        public string? Method { get; set; }
        public List<ParamItem>? Params { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public AuthenConfigXRestAPI? Authen { get; set; }
        public BodyConfig? Body { get; set; }
        public int Timeout { get; set; }
        public bool ExecuteAsync { get; set; }
        public ExceptionConfig? Exception { get; set; }
        public AfterExecuteConfig? AfterExecute { get; set; }
    }

    public class ParamItem
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class AuthenConfigXRestAPI
    {
        public AuthorType Type { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class BodyConfig
    {
        public string? Type { get; set; }            // form-data | raw | urlencoded | binary
        public string? RawFormat { get; set; }       // json | xml | text
        public string? RawContent { get; set; }
        public List<FormField>? FormFields { get; set; }
        public List<FormUrlEncodedField>? UrlEncodedFields { get; set; }
        public string? BinaryValue { get; set; }
    }

    public class FormField
    {
        public string? Id { get; set; }
        public string? Key { get; set; }
        public string? Type { get; set; }            // text | file
        public string? Value { get; set; }
    }

    public class FormUrlEncodedField
    {
        public string? Id { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
    }

    public class ExceptionConfig
    {
        public List<ExceptionHttpCode>? HttpCode { get; set; }
        public List<ExceptionBodyResponse>? BodyResponse { get; set; }
    }

    public class ExceptionHttpCode
    {
        public string? Key { get; set; }
        public string Code { get; set; }
        public string? StepId { get; set; }
    }

    public class ExceptionBodyResponse
    {
        public string? Key { get; set; }
        public string? Label { get; set; }
        public string? Value { get; set; }
        public string? StepId { get; set; }
    }

    public class AfterExecuteConfig
    {
        public string? Action { get; set; }
        public string? StepId { get; set; }
        public string? SaveTo { get; set; }
        public string? WfExecuteSuccess { get; set; }
        public string? WfExecuteError { get; set; }
    }

    public class ForwardFileSourceConfig
    {
        public string? Url { get; set; }
        public List<ParamItem>? Params { get; set; }
    }

    public class ForwardFileTargetConfig
    {
        public string? Url { get; set; }
        public string? Method { get; set; }        // POST | PUT (FE chỉ cho chọn 2 cái này)
        public List<ParamItem>? Params { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public AuthenConfig? Authen { get; set; }
        public BodyConfig? Body { get; set; }      // dùng FormFields (text) nếu Mode = form-data
    }

    public class ForwardFileWFConfig
    {
        public ForwardFileSourceConfig? Source { get; set; }
        public ForwardFileTargetConfig? Target { get; set; }

        public string? Mode { get; set; }          // "form-data" | "binary"
        public string? TargetFieldName { get; set; }
        public string? TargetFileName { get; set; }
        public int Timeout { get; set; } = 30;
    }

    

}