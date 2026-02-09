using IBox.Database.Root.Tables;
using Newtonsoft.Json;
using Serilog;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;

namespace IBox.MailService
{
    public class SMTPService : ISmtpService
    {
        public Task Send(MailServerInfo mailServerInfo, MailConfig mailConfig)
        {
            try
            {
                List<string> arrMailTo = SubToString(mailConfig.MailTo);
                List<string> arrMailCC = SubToString(mailConfig.MailCC);
                List<string> arrMailBCC = SubToString(mailConfig.MailBCC);

                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient()
                {
                    EnableSsl = true
                };

                if (!string.IsNullOrEmpty(mailServerInfo.Smtp_Username))
                {
                    message.From = new MailAddress(mailServerInfo.Smtp_Username);
                }
                else
                {
                    Log.Error("From: ");
                }

                foreach (string item in arrMailTo)
                {
                    message.To.Add(new MailAddress(item));
                }

                foreach (string item in arrMailCC)
                {
                    message.CC.Add(new MailAddress(item));
                }

                foreach (string item in arrMailBCC)
                {
                    message.Bcc.Add(new MailAddress(item));
                }

                message.Subject = string.IsNullOrEmpty(mailConfig.MailTitle) ? "" : mailConfig.MailTitle;
                message.Body = string.IsNullOrEmpty(mailConfig.MailBody) ? "" : mailConfig.MailBody;
                message.IsBodyHtml = true;
                smtp.Port = int.Parse(string.IsNullOrEmpty(mailServerInfo.Smtp_Port) ? "0" : mailServerInfo.Smtp_Port);
                smtp.Host = string.IsNullOrEmpty(mailServerInfo.Smtp_Host) ? "" : mailServerInfo.Smtp_Host;
                smtp.EnableSsl = checkNull(mailServerInfo.Smtp_EnableSSL);
                smtp.UseDefaultCredentials = false;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                if (checkNull(mailServerInfo.Smtp_Credentials))
                {
                    smtp.Credentials = new NetworkCredential(string.IsNullOrEmpty(mailServerInfo.Smtp_Username) ? "" : mailServerInfo.Smtp_Username, string.IsNullOrEmpty(mailServerInfo.Smtp_Password) ? "" : mailServerInfo.Smtp_Password);
                }

                smtp.SendMailAsync(message);
                smtp.SendCompleted += Smtp_SendCompleted;
            }
            catch (Exception ex)
            {
                Log.Error("Send Mail: " + ex + $"\n mailServerInfo: {JsonConvert.SerializeObject(mailServerInfo)}\n mailConfig: {JsonConvert.SerializeObject(mailConfig)}");
                throw;
            }
            return Task.CompletedTask;
        }

        public Task SendMailServer(T_Tenant t_Tenant)
        {
            try
            {
                List<string> arrMailTo = SubToString(t_Tenant.MailTo);
                List<string> arrMailCC = SubToString(t_Tenant.MailCC);
                List<string> arrMailBCC = SubToString(t_Tenant.MailBCC);

                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                if (!string.IsNullOrEmpty(t_Tenant.Smtp_Username))
                {
                    message.From = new MailAddress(t_Tenant.Smtp_Username);
                }
                else
                {
                    Log.Error("SendMailServer From: ");
                }

                foreach (string item in arrMailTo)
                {
                    message.To.Add(new MailAddress(item));
                }

                foreach (string item in arrMailCC)
                {
                    message.CC.Add(new MailAddress(item));
                }

                foreach (string item in arrMailBCC)
                {
                    message.Bcc.Add(new MailAddress(item));
                }

                message.Subject = string.IsNullOrEmpty(t_Tenant.MailTitle) ? "" : t_Tenant.MailTitle;
                message.Body = string.IsNullOrEmpty(t_Tenant.MailBody) ? "" : t_Tenant.MailBody;
                message.IsBodyHtml = true;
                smtp.Port = int.Parse(string.IsNullOrEmpty(t_Tenant.Smtp_Port) ? "0" : t_Tenant.Smtp_Port);
                smtp.Host = string.IsNullOrEmpty(t_Tenant.Smtp_Host) ? "" : t_Tenant.Smtp_Host;
                smtp.EnableSsl = checkNull(t_Tenant.Smtp_EnableSSL);
                smtp.UseDefaultCredentials = false;

                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                if (checkNull(t_Tenant.Smtp_Credentials))
                {
                    smtp.Credentials = new NetworkCredential(string.IsNullOrEmpty(t_Tenant.Smtp_Username) ? "" : t_Tenant.Smtp_Username, string.IsNullOrEmpty(t_Tenant.Smtp_Password) ? "" : t_Tenant.Smtp_Password);
                }

                smtp.SendMailAsync(message);
                smtp.SendCompleted += Smtp_SendCompleted;
            }
            catch (Exception ex)
            {
                Log.Error("Send Mail Server: " + ex + $"\n Request_MailServer: {JsonConvert.SerializeObject(t_Tenant)}");
                throw;
            }
            return Task.CompletedTask;
        }

        private bool checkNull(bool? value)
        {
            return value == null ? false : value.Value;
        }

        private void Smtp_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {

            Log.Information(sender.ToString(), e.Error);

        }

        private List<string> SubToString(string? data)
        {
            return string.IsNullOrEmpty(data) ? new List<string>() : data.Replace(" ", "").Split(',').ToList();
        }
    }
}