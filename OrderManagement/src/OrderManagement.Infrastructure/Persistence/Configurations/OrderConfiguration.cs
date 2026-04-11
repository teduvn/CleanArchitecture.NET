using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Configurations
{
    //public class OrderConfiguration : IEntityTypeConfiguration<Order>
    //{
    //    public void Configure(EntityTypeBuilder<Order> builder)
    //    {
    //        builder.HasKey(o => o.Id);


    //        builder.Property(o => o.Status)
    //            .HasConversion<string>();


    //        // Map Value Object Money
    //        builder.OwnsOne(o => o.ShippingAddress);


    //        // OrderItem là child — HasMany, không phải HasOne
    //        builder.HasMany("_items")
    //            .WithOne()
    //            .HasForeignKey("OrderId")
    //            .IsRequired()
    //            .OnDelete(DeleteBehavior.Cascade);


    //        // Trỏ vào backing field private
    //        builder.Navigation("_items").UsePropertyAccessMode(
    //            PropertyAccessMode.Field);
    //    }
    //}

}
