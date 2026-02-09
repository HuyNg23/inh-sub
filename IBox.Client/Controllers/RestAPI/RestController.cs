using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace IBox.Client.Controllers.RestAPI
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class RestController : ActionController<RestRequestForGadgetTool, TenantContext>
    {
        private readonly IRestAPI _restAPI;

        public RestController(IBContext<TenantContext> tenantContext, IRestAPI restAPI, IEncryption encryption) : base(tenantContext, encryption)
        {
            _restAPI = restAPI;
        }

        [IBoxActionPermission("request-tool-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            return base.Create(req);
        }

        [IBoxActionPermission("request-tool-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        [IBoxActionPermission("request-tool-manage", "request-tool-view-send")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAll(req, pageNumber);
        }

        [IBoxActionPermission("request-tool-manage", "request-tool-view-send")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        [IBoxActionPermission("request-tool-manage", "request-tool-view-send")]
        public override ResponseForm<dynamic> GetDetail(RequestForm<dynamic> req, string id)
        {
            return base.GetDetail(req, id);
        }

        [IBoxActionPermission("request-tool-manage")]
        public override ResponseForm<dynamic> RollbackDelete(RequestForm<dynamic> req)
        {
            return base.RollbackDelete(req);
        }

        [HttpPost("Send")]
        [IBoxActionPermission("request-tool-manage")]
        public ResponseForm<dynamic> SendRequest(RequestForm<RestRequestForGadgetTool> req)
        {
            return new ResponseForm<dynamic>(() =>
            {
                try
                {
                    var result = _restAPI.Send(new RestAPIRequest()
                    {
                        Url = req.Body.Url,
                        Body = req.Body.Body,
                        Headers = JsonConvert.DeserializeObject<List<RestAPIHeader>>(req.Body.Headers),
                        Method = req.Body.Method
                    }, "AppLogs");

                    return new
                    {
                        request = result?.Request,
                        Response = result?.Result
                    };
                }
                catch (Exception ex)
                {
                    throw new IboxLog($"An Unexpected Error Has Occurred {ex.Message}", "AppLogs", ex);
                }
            });
        }

        [IBoxActionPermission("request-tool-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            return base.Update(req);
        }
    }
}