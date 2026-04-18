using Microsoft.Extensions.Options;
using OrderManagement.Application.Common.Interfaces;
using Stripe;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Payment
{
    public sealed class StripePaymentGateway : IPaymentGateway
    {
        private readonly PaymentIntentService _service;

        public StripePaymentGateway(IOptions<StripeSettings> options)
        {
            StripeConfiguration.ApiKey = options.Value.SecretKey;
            _service = new PaymentIntentService();
        }

        public async Task<PaymentResult> ChargeAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var intent = await _service.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Stripe dùng đơn vị cent
                    Currency = request.Currency.ToLower(),
                    PaymentMethod = request.PaymentMethodToken,
                    Confirm = true,
                }, cancellationToken: cancellationToken);

                return intent.Status == "succeeded"
                    ? new PaymentResult(true, intent.Id, null, null)
                    : new PaymentResult(false, null, "PAYMENT_FAILED", intent.Status);
            }
            catch (StripeException ex)
            {
                return new PaymentResult(false, null, ex.StripeError?.Code, ex.Message);
            }
        }

        public async Task<PaymentResult> RefundAsync(
            string transactionId, decimal amount,
            CancellationToken cancellationToken = default)
        {
            // Implement tương tự dùng RefundService
            await Task.CompletedTask;
            return new PaymentResult(true, transactionId, null, null);
        }
    }

}
