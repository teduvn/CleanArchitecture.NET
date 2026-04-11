using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.Commands
{
    internal class PlaceOrderCommandHandler
    {
        //public async Task<Result> Handle(AddItemToOrderCommand command,
        //                          CancellationToken ct)
        //{
        //    var order = await _orderRepository.GetByIdAsync(command.OrderId, ct)
        //        ?? throw new NotFoundException(nameof(Order), command.OrderId);


        //    var product = await _productRepository.GetByIdAsync(command.ProductId, ct)
        //        ?? throw new NotFoundException(nameof(Product), command.ProductId);


        //    // Tất cả invariant được kiểm tra bên trong Order
        //    order.AddItem(
        //        product.Id,
        //        product.Name,
        //        command.Quantity,
        //        product.Price);


        //    // Chỉ save Order — EF Core cascade save OrderItem
        //    await _orderRepository.SaveAsync(ct);


        //    return Result.Success();
        //}

    }
}
