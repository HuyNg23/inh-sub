using IBox.ChatBot.DB.DBChatDay;
using IBox.ChatBot.DB.Models.ChatBot;
using IBox.ChatBot.DB.Models.CreateFile.IBox;
using IBox.ChatBot.History.History;
using IBox.ChatBot.History.Model;
using IBox.Common.Model;
using IBox.Common.Objects;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace IBox.LogService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShowDataController : ControllerBase
    {
        public readonly IChangeDBChatDay _changeDBChatDay;
        public readonly Common.Objects.IConfiguration _configuration;
        public readonly IHandelChatSessionDaily _handelChatSessionDaily;

        public ShowDataController(IChangeDBChatDay changeDBChatDay, IHandelChatSessionDaily handelChatSessionDaily, Common.Objects.IConfiguration configuration)
        {
            _changeDBChatDay = changeDBChatDay;
            _handelChatSessionDaily = handelChatSessionDaily;
            _configuration = configuration;
        }

        [HttpPost]
        [Route("ShowDataChatBot")]
        public ResponseForm<ListDataIBox> ShowDataChatBot(RequestForm<RequestShowData> requestShowData)
        {
            var result = _changeDBChatDay.ShowDataChatBot(requestShowData.Body.senderId, requestShowData.Body.tenantId, requestShowData.Body.sessionId, requestShowData.Body.dateTimeCurrent);
            return new ResponseForm<ListDataIBox>(() =>
            {
                return result;
            });
        }

        [HttpPost]
        [Route("GetDataCustomer")]
        public ResponseForm<CB_Customer> GetDataCustomer(RequestForm<RequestShowData> requestShowData)
        {
            var result = _changeDBChatDay.GetDataCustomer(requestShowData.Body.tenantId, requestShowData.Body.senderId, requestShowData.Body.idBot);
            return new ResponseForm<CB_Customer>(() =>
            {
                return result;
            });
        }

        [HttpGet]
        [Route("GetCustomer/{tenantId}/{senderId}/{idBot}")]
        public CB_Customer GetCustomer(string tenantId, string senderId, string? idBot)
        {
            return _changeDBChatDay.GetCustomer(tenantId, senderId, idBot);
        }

        [HttpPost]
        [Route("ChatBotSendIC")]
        public ResponseForm<string> ChatBotSendIC(RequestForm<RequestShowData> requestShowData)
        {
            var config = IBGlobalTenantConfig.ConfigChatBots.FirstOrDefault(ptr => ptr.tenant_id == requestShowData.Body.tenantId);
            _changeDBChatDay.ChatBotSendIC(requestShowData.Body);

            if (config == null)
            {
                return new ResponseForm<string>(() =>
                {
                    return "config is not null";
                });
            }

            return new ResponseForm<string>(() =>
            {
                return config.Url_IC;
            });
        }

        [HttpPost]
        [Route("ShowAllDataChatBot")]
        public ResponseForm<List<ResponseChatBotGetSenderId>> ShowAllDataChatBot(RequestShowData requestShowData)
        {
            var result = _changeDBChatDay.ShowAllDataChatBot(requestShowData);
            return new ResponseForm<List<ResponseChatBotGetSenderId>>(() =>
            {
                return result;
            });
        }

        [HttpPost]
        [Route("ShowHistorySessionChat")]
        [IBoxAuthorization]
        public ResponseGetChatSessionDaily ShowHistorySessionChat(RequestGetChatSessionDaily requestGetChatSessionDaily)
        {
            Log.Information("ShowHistorySessionChat");
            requestGetChatSessionDaily.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return _handelChatSessionDaily.GetDataChatSession(requestGetChatSessionDaily);
        }

        [HttpPost]
        [Route("ShowHistoryGetChatSessionDaily")]
        [IBoxAuthorization]
        public ResponseForm<ResponseGetChatSessionDaily> ShowHistoryGetChatSessionDaily(RequestForm<RequestGetChatSessionDaily> requestGetChatSessionDaily)
        {
            Log.Information("ShowHistoryGetChatSessionDaily");
            var Authorize = Request.Headers["Authorization"];
            requestGetChatSessionDaily.Body.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            var result = _handelChatSessionDaily.GetChatSessionDaily(requestGetChatSessionDaily.Body, Authorize.ToString());

            return new ResponseForm<ResponseGetChatSessionDaily>(() =>
            {
                return result;
            });
        }

        [HttpPost]
        [Route("ShowHistoryMessageChat")]
        [IBoxAuthorization]
        public ResponseGetChatMessageDaily ShowHistoryMessageChat(RequestGetChatMessageDaily requestGetChatMessageDaily)
        {
            requestGetChatMessageDaily.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return _handelChatSessionDaily.GetDataChatMessage(requestGetChatMessageDaily);
        }

        [HttpPost]
        [Route("ShowHistoryGetChatMessageDaily")]
        [IBoxAuthorization]
        public ResponseForm<ResponseGetChatMessageDaily> ShowHistoryGetChatMessageDaily(RequestForm<RequestGetChatMessageDaily> requestGetChatMessageDaily)
        {
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
            }

            var Authorize = Request.Headers["Authorization"];
            requestGetChatMessageDaily.Body.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            var result = _handelChatSessionDaily.GetChatMessageDaily(requestGetChatMessageDaily.Body, Authorize.ToString());

            return new ResponseForm<ResponseGetChatMessageDaily>(() =>
            {
                return result;
            });
        }

        [HttpPost]
        [Route("ShowHistoryZipFile")]
        [IBoxAuthorization]
        public ResponseForm<ResponseHistoryZipFile> ShowHistoryZipFile(RequestHistoryZipFile requestHistoryZipFile)
        {
            return new ResponseForm<ResponseHistoryZipFile>(()
                => _handelChatSessionDaily.ShowHistoryZipFile(requestHistoryZipFile));
        }

        [HttpGet]
        [Route("ShowSizeDriveFolder")]
        public ResponseForm<ResponseSizeFolder> ShowSizeDriveFolder(TypeUserBase typeUserBase)
        {
            return new ResponseForm<ResponseSizeFolder>(()
                => _handelChatSessionDaily.ShowSizeDriveFolder(typeUserBase));
        }

        [HttpPost]
        [Route("GetAllSizeDriveFolderChatBot")]
        [IBoxAuthorization]
        public ResponseForm<List<ResponseSizeFolder>> GetAllSizeDriveFolderChatBot()
        {
            return new ResponseForm<List<ResponseSizeFolder>>(()
                => _handelChatSessionDaily.GetAllSizeDriveFolderChatBot(HttpContext.Request));
        }

        [HttpPost]
        [Route("ShowSizeDriveFolderOnRing")]
        [IBoxAuthorization]
        public ResponseForm<ResponseSizeFolder> ShowSizeDriveFolderOnRing()
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
            return new ResponseForm<ResponseSizeFolder>(()
                => _handelChatSessionDaily.ShowSizeDriveFolderOnRing(tenantId));
        }

        [HttpPost]
        [Route("ShowHistorySenderDaily")]
        [IBoxAuthorization]
        public ResponseForm<ResponseGetSenderDaily> ShowHistorySenderDaily(RequestForm<RequestGetSender> requestGetChatMessageDaily)
        {
            try
            {
                var Authorize = Request.Headers["Authorization"];
                requestGetChatMessageDaily.Body.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                var result = _handelChatSessionDaily.GetSenderDaily(requestGetChatMessageDaily.Body, Authorize);
                if (!HttpContext.Response.HasStarted)
                {
                    HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
                }

                return new ResponseForm<ResponseGetSenderDaily>(() =>
                {
                    return result;
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        [Route("ShowHistorySender")]
        [IBoxAuthorization]
        public ResponseGetSenderDaily ShowHistorySender(RequestGetSender requestGetChatMessageDaily)
        {
            try
            {
                if (!HttpContext.Response.HasStarted)
                {
                    HttpContext.Response.Headers.TryAdd("Site", IBGlobalConfig.ThisSite);
                }

                requestGetChatMessageDaily.TenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;
                return _handelChatSessionDaily.GetSender(requestGetChatMessageDaily);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}