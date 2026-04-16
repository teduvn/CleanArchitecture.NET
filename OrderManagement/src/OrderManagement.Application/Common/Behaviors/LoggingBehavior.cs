using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace OrderManagement.Application.Common.Behaviors
{
    // Generic constraint: áp dụng cho TẤT CẢ command và query
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(
            ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            // Trước khi handler chạy: log tên request
            _logger.LogInformation(
                "[MediatR] Handling {RequestName}: {@Request}",
                requestName, request);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Gọi behavior/handler tiếp theo trong pipeline
                var response = await next();

                stopwatch.Stop();

                // Sau khi handler chạy xong: log kết quả + thời gian
                _logger.LogInformation(
                    "[MediatR] {RequestName} handled in {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "[MediatR] {RequestName} failed after {ElapsedMs}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
                throw; // Không nuốt exception — chỉ log rồi rethrow
            }
        }
    }

}
