using IBox.Common.Objects;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;
using System.Net.Http.Headers;

namespace IBox.Root.Client.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxRootAuthorization]
    public class ServiceController : ActionController<S_Service, RootContext>
    {
        private readonly IRestAPI _restAPI;
        private readonly IBContext<RootContext> _rootContext;

        public ServiceController(IBContext<RootContext> context, IRestAPI restAPI, IBContext<RootContext> rootContext) : base(context)
        {
            _restAPI = restAPI;
            _rootContext = rootContext;
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var serviceModel = CheckValid(req);

                var create = base.Create(req);

                if (serviceModel.TypeService == TypeService.RootBE || serviceModel.TypeService == TypeService.ScheduleService)
                {
                    CallApiReLoadSqlDependencyOnRing(this.Request, serviceModel);
                }

                return create;
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var serviceModel = CheckValid(req);
                var tenantID = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                this._restAPI.SendMailAlert(IBGlobalConfig.LogServiceIBox, new
                {
                    Id = tenantID,
                    MailTitle = "[Warning] Service IBox has been changed",
                    MailBody = $"{serviceModel.Name}",
                    typeWarning = TypeWarning.ChangeConfig
                });

                var data = _rootContext.Context.S_Services.FirstOrDefault(ptr => ptr.Id == serviceModel.Id && !ptr.IsDelete);

                if (data == null)
                {
                    throw new IboxLog($"Does not exist Tenant: {serviceModel.Id}", tenantID);
                }

                var update = base.Update(req);

                if (serviceModel.TypeService == TypeService.RootBE || serviceModel.TypeService == TypeService.ScheduleService)
                {
                    CallApiReLoadSqlDependencyOnRing(this.Request, serviceModel);
                }

                return update;
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        private void CallApiReLoadSqlDependencyOnRing(HttpRequest request, S_Service service)
        {
            try
            {
                var authorize = request.Headers["Authorization"].ToString();
                var url = buildURL(service);

                Task.Run(() =>
                {
                    var rest = string.Format(@"{0}{1}", url, "/api/ListenTableDependency/ReLoadSqlDependency");
                    var handler = new HttpClientHandler();
                    handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                    HttpClient httpClient = new HttpClient(handler);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, rest);
                    requestMessage.Headers.Add("Authorization", authorize);
                    httpClient.SendAsync(requestMessage).Wait();
                });
            }
            catch (Exception ex)
            {
                Log.Error($"CallApiReLoadSqlDependencyOnRing error because: {ex}");
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
    }
}