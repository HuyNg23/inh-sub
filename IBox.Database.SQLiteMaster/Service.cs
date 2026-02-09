using Microsoft.Extensions.DependencyInjection;

namespace IBox.Database.SQLiteMaster
{
    public static class Service
    {
        public static IServiceCollection AddServiceSqliteDB(this IServiceCollection services)
        {
            services.AddScoped<IDBChatMasterContext<DBChatMasterContext>, DBChatMasterContext>();
            return services;
        }
    }
}