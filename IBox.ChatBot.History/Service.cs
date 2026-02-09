using IBox.ChatBot.History.History;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.ChatBot.History
{
    public static class Service
    {
        public static IServiceCollection AddServiceChatBotHistory(this IServiceCollection services)
        {
            services.AddScoped<IHandelChatSessionDaily, HandelChatSessionDaily>();
            return services;
        }
    }
}