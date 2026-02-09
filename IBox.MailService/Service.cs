using Microsoft.Extensions.DependencyInjection;

namespace IBox.MailService
{
    public static class Service
    {
        public static IServiceCollection AddServiceMail(this IServiceCollection services)
        {
            services.AddScoped<ISmtpService, SMTPService>();
            services.AddScoped<IServiceMailAlert, ServiceMailAlert>();
            return services;
        }
    }
}