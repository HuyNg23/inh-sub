using IBox.ChatBot.Service;
using IBox.ChatBot.Service.BOT;
using IBox.ChatBot.Service.CustomerInfo;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.ChatBot.Handel
{
    public static class Service
    {
        public static IServiceCollection AddServiceChatBotHandel(this IServiceCollection services)
        {
            services.AddScoped<IServiceBot, ServiceBot>();
            services.AddScoped<IInfoCustomerService, InfoCustomerService>();
            services.AddScoped<IFileProcessing, FileProcessing>();
            return services;
        }
    }
}