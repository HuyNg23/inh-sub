using IBox.Common.Chat.CommonData.CallData;
using IBox.Common.Chat.Configuration;
using IBox.Common.FolderLog;
using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Common.Version;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Common
{
    public static class Service
    {
        public static IServiceCollection AddServiceCommon(this IServiceCollection services)
        {
            services.AddScoped<IConfiguration, Configuration>();
            services.AddScoped<IEncryption, XEncryption>();
            services.AddScoped<IRestAPI, RestApi>();
            services.AddScoped<ICheckVersion, CheckVersion>();
            services.AddScoped<ICallData, CallData>();
            services.AddScoped<ICommonData, CommonData>();
            services.AddSingleton<IRestLog, RestLog>();
            return services;
        }
    }
}