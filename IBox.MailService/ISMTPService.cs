using IBox.Database.Root.Tables;

namespace IBox.MailService
{
    public interface ISmtpService
    {
        Task Send(MailServerInfo mailServerInfo, MailConfig mailConfig);

        Task SendMailServer(T_Tenant t_Tenant);
    }
}