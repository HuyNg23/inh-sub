using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root.Tables;

namespace IBox.MailService
{
    public interface IServiceMailAlert
    {
        T_Tenant Body(MailAlertRootConfig mailConfigInfo, IConfiguration _configuration, IEncryption _encryption);
    }
}