using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using IBox.WebSocket.Model;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.WebSocket
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    public class EventWebSocketController : ActionController<W_EventSocket, TenantContext>
    {
        public EventWebSocketController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var W_EventSocketsModel = CheckValid(req);
                if (string.IsNullOrEmpty(W_EventSocketsModel.EventName?.Trim()))
                {
                    throw new IboxLog("EventName can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_EventSocketsModel.AddressSocket?.Trim()))
                {
                    throw new IboxLog("AddressSocket can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_EventSocketsModel.WorkflowId?.Trim()))
                {
                    throw new IboxLog("WorkflowId can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_EventSocketsModel.WebSocketId?.Trim()))
                {
                    throw new IboxLog("WebSocket Id can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.W_EventSockets.
                    FirstOrDefault(ptr =>
                    ptr.EventName == W_EventSocketsModel.EventName
                    && ptr.AddressSocket == W_EventSocketsModel.AddressSocket
                    && ptr.WorkflowId == W_EventSocketsModel.WorkflowId
                    && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Event Sockets name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                return base.Create(req);
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
                var W_EventSocketsModel = CheckValid(req);
                if (string.IsNullOrEmpty(W_EventSocketsModel.EventName?.Trim()))
                {
                    throw new IboxLog("Event Name can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_EventSocketsModel.AddressSocket?.Trim()))
                {
                    throw new IboxLog("Address Socket can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_EventSocketsModel.WorkflowId?.Trim()))
                {
                    throw new IboxLog("Workflow Id can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.W_EventSockets.
                    FirstOrDefault(ptr =>
                    ptr.EventName == W_EventSocketsModel.EventName
                    && ptr.WorkflowId == W_EventSocketsModel.WorkflowId
                    && ptr.Id != W_EventSocketsModel.Id
                    && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Event Sockets name already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetAllGroupWebSocket")]
        public ResponseForm<List<ResW_WebSockets>> GetAllGroupWebSocket(RequestForm<ReqW_WebSockets> req)
        {
            try
            {
                var query = from ws in base.IBContext.Context.W_WebSockets
                            join eWs in base.IBContext.Context.W_EventSockets on ws.Id equals eWs.WebSocketId into leftjEventSocket
                            from eWs in leftjEventSocket.DefaultIfEmpty()
                            where !ws.IsDelete && !eWs.IsDelete
                            select new
                            {
                                ws,
                                eWs
                            };



                var filteredQuery = query.AsEnumerable().Where(filter =>
                            filter.eWs == null ? (string.IsNullOrEmpty(req.Body.Value) || filter.ws.Name.Contains(req.Body.Value)) : (string.IsNullOrEmpty(req.Body.Value)
                            || filter.ws.Name.Contains(req.Body.Value)
                            || filter.eWs.EventName.Contains(req.Body.Value)
                            ));



                var result = filteredQuery
                            .GroupBy(gr => new
                            {
                                gr.ws.Name,
                                gr.ws.Domain,
                                gr.ws.Port,
                                gr.ws.Site,
                                gr.ws.Id,
                                gr.ws.CreatedDate,
                                gr.ws.IsDelete,
                                gr.ws.ModificationDate
                            }).Select(ptr => new ResW_WebSockets
                            {
                                Name = ptr.Key.Name,
                                Domain = ptr.Key.Domain,
                                Site = ptr.Key.Site,
                                Port = ptr.Key.Port,
                                Id = ptr.Key.Id,
                                CreatedDate = ptr.Key.CreatedDate,
                                IsDelete = ptr.Key.IsDelete,
                                ModificationDate = ptr.Key.ModificationDate,
                                ListEventSocket = ptr.Where(filter => filter.eWs != null).Select(ptr => new W_EventSocket
                                {
                                    Id = ptr.eWs.Id,
                                    EventName = ptr.eWs.EventName,
                                    WorkflowId = ptr.eWs.WorkflowId,
                                    WebSocketId = ptr.eWs.WebSocketId,
                                    AddressSocket = ptr.eWs.AddressSocket,
                                    CreatedDate = ptr.eWs.CreatedDate,
                                }).ToList()
                            });

                return new ResponseForm<List<ResW_WebSockets>>(() => result.ToList());
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<ResW_WebSockets>>(() => throw ex);
            }
        }
    }
}