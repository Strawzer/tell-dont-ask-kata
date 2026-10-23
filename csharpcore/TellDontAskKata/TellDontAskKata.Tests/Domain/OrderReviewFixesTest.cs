using System;
using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using Xunit;

namespace TellDontAskKata.Tests.Domain
{
    public class OrderReviewFixesTest
    {
        private static readonly Category Food = new Category("food", 10m);
        private static readonly Product Salad = new Product("salad", 3.56m, Food);
        private static readonly Product Tomato = new Product("tomato", 4.65m, Food);

        // Pins public numeric values; note default(OrderStatus) is Approved (known debt).
        [Theory]
        [InlineData(OrderStatus.Approved, 0)]
        [InlineData(OrderStatus.Rejected, 1)]
        [InlineData(OrderStatus.Shipped, 2)]
        [InlineData(OrderStatus.Created, 3)]
        public void OrderStatusNumericValuesArePinned(OrderStatus status, int expectedValue)
        {
            Assert.Equal(expectedValue, (int)status);
        }

        [Fact]
        public void RestoreKeepsStateAndRecomputesTotalsFromItems()
        {
            var saladItem = new OrderItem(Salad, 2);
            var tomatoItem = new OrderItem(Tomato, 3);

            var order = Order.Restore(7, OrderStatus.Approved, "EUR", new List<OrderItem> { saladItem, tomatoItem });

            Assert.Equal(7, order.Id);
            Assert.Equal(OrderStatus.Approved, order.Status);
            Assert.Equal("EUR", order.Currency);
            Assert.Equal(2, order.Items.Count);
            Assert.Same(saladItem, order.Items[0]);
            Assert.Same(tomatoItem, order.Items[1]);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(2.13m, order.Tax);
        }

        [Fact]
        public void RestoredShippedOrderCannotBeApproved()
        {
            var order = Order.Restore(1, OrderStatus.Shipped, "EUR", new List<OrderItem>());

            Assert.Throws<ShippedOrdersCannotBeChangedException>(() => order.Approve());
            Assert.Equal(OrderStatus.Shipped, order.Status);
        }
    }
}
