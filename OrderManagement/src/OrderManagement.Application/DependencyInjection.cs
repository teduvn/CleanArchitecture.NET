using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Common.Behaviors;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OrderManagement.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Quét tất cả Handler trong Assembly này và đăng ký
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                // Đăng ký behavior vào pipeline
                // Thứ tự quan trọng: Behavior đăng ký trước chạy trước
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // Đăng ký tất cả validator trong assembly này
            // Quét PlaceOrderCommandValidator, OrderItemDtoValidator, ... tự động
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            return services;
        }

    }
}
