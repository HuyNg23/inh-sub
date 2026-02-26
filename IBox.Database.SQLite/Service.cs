using IBox.ChatBot.HandleScheduleSQL;
using IBox.Database.SQLiteDBChatDay;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Database.ChatBot
{
    public static class Service
    {
        public static IServiceCollection AddServiceSqliteDB(this IServiceCollection services)
        {
            services.AddScoped<ICreateDB, CreateDB>();
            services.AddTransient<DBChatDayContextFactory>();
            services.AddScoped<IDBChatDayContext<DBChatDayContext>, DBChatDayContext>();

            services.AddTransient<DBHistoryContextFactory>();
            services.AddScoped<IDBHistoryContext<DBHistoryContext>, DBHistoryContext>();
            return services;
        }
    }
}