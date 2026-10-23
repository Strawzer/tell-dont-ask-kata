using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class OrderAddItemTest
    {
        private static readonly Category Food = new Category("food", 10m);
        private static readonly Product Salad = new Product("salad", 3.56m, Food);
        private static readonly Product Tomato = new Product("tomato", 4.65m, Food);

        [Fact]
        public void AddingItemToShippedOrderThrowsAndLeavesOrderUnchanged()
        {
            var existingItem = new OrderItem(Salad, 2);
            var order = Order.Restore(1, OrderStatus.Shipped, "EUR", new List<OrderItem> { existingItem });

            Assert.Throws<ShippedOrdersCannotBeChangedException>(() => order.AddItem(Tomato, 3));

            Assert.Single(order.Items);
            Assert.Same(existingItem, order.Items[0]);
            Assert.Equal(7.84m, order.Total);
            Assert.Equal(0.72m, order.Tax);
            Assert.Equal(OrderStatus.Shipped, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Created)]
        [InlineData(OrderStatus.Approved)]
        [InlineData(OrderStatus.Rejected)]
        public void AddingItemToUnshippedOrderIsAllowed(OrderStatus status)
        {
            var order = Order.Restore(1, status, "EUR", new List<OrderItem> { new OrderItem(Salad, 2) });

            order.AddItem(Tomato, 3);

            Assert.Equal(2, order.Items.Count);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(2.13m, order.Tax);
            Assert.Equal(status, order.Status);
        }

        [Fact]
        public void RestoreStillAcceptsShippedOrderWithItems()
        {
            var order = Order.Restore(
                1, OrderStatus.Shipped, "EUR", new List<OrderItem> { new OrderItem(Salad, 2), new OrderItem(Tomato, 3) });

            Assert.Equal(OrderStatus.Shipped, order.Status);
            Assert.Equal(2, order.Items.Count);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(2.13m, order.Tax);
        }
    }
}
