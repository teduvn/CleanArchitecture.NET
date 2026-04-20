
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderManagement.Application.Common.Interfaces;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Infrastructure.Email;
using OrderManagement.Infrastructure.FileStorage;
using OrderManagement.Infrastructure.Payment;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Persistence.Seeding;

namespace OrderManagement.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration, IHostEnvironment env)
        {

            // Đăng ký DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(
                            typeof(ApplicationDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(   // built-in retry cho transient error
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    }));

            // Đăng ký seeders
            services.AddScoped<IDataSeeder, RoleSeedDataSeeder>();

            // Development seeder chỉ đăng ký khi chạy dev environment
            if (env.IsDevelopment())
            {
                services.AddScoped<IDataSeeder, DevelopmentOrderSeeder>();
            }


            // Map interface IUnitOfWork sang ApplicationDbContext
            // Scoped để share instance trong cùng 1 request
            services.AddScoped<IUnitOfWork>(
                sp => sp.GetRequiredService<ApplicationDbContext>());


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
