using IBox.ChatBot.DB.DBChatDay;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.ChatBot.DB
{
    public static class Service
    {
        public static IServiceCollection AddServiceChatBotDB(this IServiceCollection services)
        {
            services.AddScoped<IChangeDBChatDay, ChangeDBChatDay>();
            return services;
        }
    }
}