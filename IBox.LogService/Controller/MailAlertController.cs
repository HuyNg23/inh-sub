using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root.Tables;
using IBox.MailService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Text;

namespace IBox.LogService.Controller
{
    [Route("api/[controller]")]
    public class MailAlertController : ControllerBase
    {
        private readonly ISmtpService _smtpService;
        private readonly Common.Objects.IConfiguration _configuration;
        private readonly IEncryption _encryption;
        private readonly IServiceMailAlert _serviceMailAlert;

        public MailAlertController(ISmtpService smtpService, Common.Objects.IConfiguration configuration, IEncryption encryption, IServiceMailAlert serviceMailAlert)
        {
            _smtpService = smtpService;
            _configuration = configuration;
            _encryption = encryption;
            _serviceMailAlert = serviceMailAlert;
        }

        [Route("SendMailAlert")]
        [HttpPost]
        public async Task SendMailAlert()
        {
            try
            {
                if (!this.HttpContext.Request.Body.CanSeek)
                {
                    this.HttpContext.Request.EnableBuffering();
                }
                var reader = new StreamReader(this.HttpContext.Request.Body, Encoding.UTF8);

                var message = await reader.ReadToEndAsync().ConfigureAwait(false);
                string content = message != "" ? JToken.Parse(message).ToString() : "";
                Log.Information("SendMailAlert API: ", content);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                this.HttpContext.Response.CompleteAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                if (string.IsNullOrEmpty(content))
                {
                    throw new IboxLog("Content of API third Party is null", "AppLogs");
                }

                var mailConfigInfo = JsonConvert.DeserializeObject<MailAlertRootConfig>(content);

                if (mailConfigInfo == null)
                {
                    throw new IboxLog("MailConfigInfo is null", "AppLogs");
                }

                var tenant = new T_Tenant();
                tenant = _serviceMailAlert.Body(mailConfigInfo, _configuration, _encryption);
                tenant.MailTitle = string.IsNullOrEmpty(tenant.MailTitle) ? mailConfigInfo.MailTitle : tenant.MailTitle;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                _smtpService.SendMailServer(tenant);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Send Mail Alert Get Request ", ex);
            }

        }
    }
}