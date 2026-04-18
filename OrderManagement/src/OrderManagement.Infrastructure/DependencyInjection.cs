
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Infrastructure.Email;
using OrderManagement.Infrastructure.FileStorage;
using OrderManagement.Infrastructure.Payment;

namespace OrderManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Đăng ký implementation cho interface từ Application
            services.AddScoped<IEmailService, SendGridEmailService>();
            services.AddScoped<IPaymentGateway, StripePaymentGateway>();
            services.AddScoped<IFileStorage, AzureBlobFileStorage>();

            // Cấu hình từ appsettings.json
            services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));
            services.Configure<StripeSettings>(configuration.GetSection("Stripe"));

            return services;
        }
    }

}
