using Microsoft.Extensions.Options;
using OrderManagement.Application.Common.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace OrderManagement.Infrastructure.Email
{
    public sealed class SendGridEmailService : IEmailService
    {
        private readonly SendGridClient _client;
        private readonly string _fromEmail;

        public SendGridEmailService(IOptions<SendGridSettings> options)
        {
            _client = new SendGridClient(options.Value.ApiKey);
            _fromEmail = options.Value.FromEmail;
        }

        public async Task SendOrderConfirmationAsync(
            string toEmail, string customerName,
            Guid orderId, decimal totalAmount,
            CancellationToken cancellationToken = default)
        {
            var msg = MailHelper.CreateSingleEmail(
                from: new EmailAddress(_fromEmail, "TEDU Shop"),
                to: new EmailAddress(toEmail, customerName),
                subject: $"Xac nhan don hang #{orderId}",
                plainTextContent: $"Cam on {customerName}! Tong tien: {totalAmount:N0} VND",
                htmlContent: null);

            await _client.SendEmailAsync(msg, cancellationToken);
        }

        public async Task SendShippingNotificationAsync(
            string toEmail, string customerName,
            Guid orderId, string trackingNumber,
            CancellationToken cancellationToken = default)
        {
            // Tương tự — build SendGridMessage, call API
            await Task.CompletedTask;
        }
    }

}
