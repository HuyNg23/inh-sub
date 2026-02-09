using IBox.ChatBot.DB.Models.FPT;
using IBox.ChatBot.DB.Models.IC;

namespace IBox.ChatBot.Service.CustomerInfo
{
    public interface IInfoCustomerService
    {
        public void IdentificationCustomer(InfoCustomer infoCustomer, string tenantId, string idBot);

        public void CreateSessionIC(CreateSessionIC infoCustomer, string tenantId, string idBot);
    }
}