using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.WebSocket
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    public class WebSocketController : ActionController<W_WebSocket, TenantContext>
    {
        public WebSocketController(IBContext<TenantContext> tenantContext, IEncryption encryption) : base(tenantContext, encryption)
        {
        }

        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
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
                var W_WebSocketModel = CheckValid(req);

                if (string.IsNullOrEmpty(W_WebSocketModel.Domain?.Trim()))
                {
                    throw new IboxLog("Domain can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_WebSocketModel.Site?.Trim()))
                {
                    throw new IboxLog("Domain can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(W_WebSocketModel.Name?.Trim()))
                {
                    throw new IboxLog("Port can't null or empty.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.W_WebSockets.FirstOrDefault(ptr => ptr.Domain == W_WebSocketModel.Domain && ptr.Site == W_WebSocketModel.Site && ptr.Port == W_WebSocketModel.Port && ptr.Id != W_WebSocketModel.Id && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("WebSocket already exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }
    }
}