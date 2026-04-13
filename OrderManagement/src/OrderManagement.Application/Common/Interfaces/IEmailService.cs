using OrderManagement.Domain.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(
            string to,
            Guid orderId,
            decimal totalAmount,
            IReadOnlyList<OrderItemSnapshot> items,
            CancellationToken cancellationToken);
    }
}
