using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.MailServer
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class MailServerController : ControllerBase
    {
        private readonly IEncryption _encryption;
        public readonly Common.Objects.IConfiguration _configuration;

        public MailServerController(IEncryption encryption, Common.Objects.IConfiguration configuration)
        {
            _encryption = encryption;
            _configuration = configuration;
        }

        [HttpGet("GetMailServerInfo")]
        [IBoxActionPermission("integration-config-mailServer-manage", "integration-config-mailServer-view")]
        public ResponseForm<MailServerInfo> GellAllEmailInPage()
        {
            var tenantId = Request.Headers.FirstOrDefault(ptr => ptr.Key == RequestHeaderKey.Tenant.ToString()).Value.ToString() ?? string.Empty;

            return new ResponseForm<MailServerInfo>(() =>
            {
                var rootContext = new RootContext(_configuration, _encryption);

                var mailServer = rootContext.T_Tenants.Where(ptr => ptr.Id == tenantId && !ptr.IsDelete).FirstOrDefault();

                rootContext.Dispose();

                if (mailServer == null)
                {
                    throw new IboxLog("Can not found mail server info for tenant.", tenantId ?? "AppLogs");
                }

                var mailServerInfo = new MailServerInfo()
                {
                    Account = mailServer.Smtp_Username ?? string.Empty,
                    Host = mailServer.Smtp_Host ?? string.Empty,
                    MailTo = mailServer.MailTo ?? string.Empty,
                    MailCC = mailServer.MailCC ?? string.Empty,
                    MailBCC = mailServer.MailBCC ?? string.Empty,
                    MailTitle = mailServer.MailTitle ?? string.Empty,
                };

                return mailServerInfo;
            });
        }
    }

    public class MailServerInfo
    {
        public string Host { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string MailTo { get; set; } = string.Empty;
        public string MailCC { get; set; } = string.Empty;
        public string MailBCC { get; set; } = string.Empty;
        public string MailTitle { get; set; } = string.Empty;
    }
}