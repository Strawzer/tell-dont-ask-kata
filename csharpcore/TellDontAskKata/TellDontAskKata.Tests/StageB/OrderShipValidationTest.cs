using System;
using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class OrderShipValidationTest
    {
        private static readonly Product Salad = new Product("salad", 3.56m, new Category("food", 10m));

        [Fact]
        public void ShippingApprovedOrderWithNullServiceThrowsArgumentNullAndLeavesOrderApproved()
        {
            var order = Order.Restore(1, OrderStatus.Approved, "EUR", new List<OrderItem> { new OrderItem(Salad, 1) });

            var exception = Assert.Throws<ArgumentNullException>(() => order.Ship(null));

            Assert.Equal("shipmentService", exception.ParamName);
            Assert.Equal(OrderStatus.Approved, order.Status);
        }

        // The null check runs after the status guards so domain exceptions keep their precedence.
        [Theory]
        [InlineData(OrderStatus.Shipped, typeof(OrderCannotBeShippedTwiceException))]
        [InlineData(OrderStatus.Created, typeof(OrderCannotBeShippedException))]
        [InlineData(OrderStatus.Rejected, typeof(OrderCannotBeShippedException))]
        public void StatusGuardsTakePrecedenceOverNullService(OrderStatus status, Type expectedException)
        {
            var order = Order.Restore(1, status, "EUR", new List<OrderItem>());

            Assert.Throws(expectedException, () => order.Ship(null));
            Assert.Equal(status, order.Status);
        }
    }
}
