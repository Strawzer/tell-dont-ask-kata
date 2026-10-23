using System;
using System.Linq;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Service;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Builders;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.Domain
{
    public class OrderTest
    {
        private static readonly Category Food = new Category("food", 10m);
        private static readonly Category Drinks = new Category("drinks", 20m);
        private static readonly Category Books = new Category("books", 0m);

        [Fact]
        public void CreateStartsAnEmptyEuroOrder()
        {
            var order = Order.Create();

            Assert.Equal(OrderStatus.Created, order.Status);
            Assert.Equal("EUR", order.Currency);
            Assert.Equal(0m, order.Total);
            Assert.Equal(0m, order.Tax);
            Assert.Empty(order.Items);
            Assert.Equal(0, order.Id);
        }

        [Fact]
        public void CreateReturnsIndependentOrders()
        {
            var first = Order.Create();
            var second = Order.Create();

            first.AddItem(new Product("salad", 3.56m, Food), 1);
            first.Approve();

            Assert.Empty(second.Items);
            Assert.Equal(0m, second.Total);
            Assert.Equal(OrderStatus.Created, second.Status);
        }

        [Fact]
        public void TotalAndTaxAreSumsOfItemAmountsAfterSeveralAdds()
        {
            var order = Order.Create();

            order.AddItem(new Product("salad", 3.56m, Food), 2);
            order.AddItem(new Product("juice", 1.01m, Drinks), 3);
            order.AddItem(new Product("novel", 12.49m, Books), 1);
            order.AddItem(new Product("salad", 3.56m, Food), 1);

            Assert.Equal(4, order.Items.Count);
            Assert.Equal(order.Items.Sum(i => i.TaxedAmount), order.Total);
            Assert.Equal(order.Items.Sum(i => i.Tax), order.Tax);
            // 7.84 + 3.66 + 12.49 + 3.92
            Assert.Equal(27.91m, order.Total);
            // 0.72 + 0.63 + 0 + 0.36
            Assert.Equal(1.71m, order.Tax);
        }

        [Fact]
        public void AddItemAppendsItemsInCallOrder()
        {
            var salad = new Product("salad", 3.56m, Food);
            var juice = new Product("juice", 1.01m, Drinks);
            var order = Order.Create();

            order.AddItem(juice, 3);
            order.AddItem(salad, 2);

            Assert.Same(juice, order.Items[0].Product);
            Assert.Equal(3, order.Items[0].Quantity);
            Assert.Same(salad, order.Items[1].Product);
            Assert.Equal(2, order.Items[1].Quantity);
        }

        [Theory]
        [InlineData(OrderStatus.Created, OrderStatus.Approved)]
        [InlineData(OrderStatus.Approved, OrderStatus.Approved)]
        public void ApproveSucceeds(OrderStatus initial, OrderStatus expected)
        {
            var order = TestOrders.WithStatus(initial);

            order.Approve();

            Assert.Equal(expected, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Rejected, typeof(RejectedOrderCannotBeApprovedException))]
        [InlineData(OrderStatus.Shipped, typeof(ShippedOrdersCannotBeChangedException))]
        public void ApproveFailsAndLeavesStatusUnchanged(OrderStatus initial, Type expectedException)
        {
            var order = TestOrders.WithStatus(initial);

            Assert.Throws(expectedException, () => order.Approve());
            Assert.Equal(initial, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Created, OrderStatus.Rejected)]
        [InlineData(OrderStatus.Rejected, OrderStatus.Rejected)]
        public void RejectSucceeds(OrderStatus initial, OrderStatus expected)
        {
            var order = TestOrders.WithStatus(initial);

            order.Reject();

            Assert.Equal(expected, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Approved, typeof(ApprovedOrderCannotBeRejectedException))]
        [InlineData(OrderStatus.Shipped, typeof(ShippedOrdersCannotBeChangedException))]
        public void RejectFailsAndLeavesStatusUnchanged(OrderStatus initial, Type expectedException)
        {
            var order = TestOrders.WithStatus(initial);

            Assert.Throws(expectedException, () => order.Reject());
            Assert.Equal(initial, order.Status);
        }

        [Fact]
        public void ShipApprovedOrderCallsShipmentServiceOnceAndMarksShipped()
        {
            var shipmentService = new CountingShipmentService();
            var order = TestOrders.WithStatus(OrderStatus.Approved);

            order.Ship(shipmentService);

            Assert.Equal(1, shipmentService.Calls);
            Assert.Same(order, shipmentService.LastOrder);
            Assert.Equal(OrderStatus.Shipped, order.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Created, typeof(OrderCannotBeShippedException))]
        [InlineData(OrderStatus.Rejected, typeof(OrderCannotBeShippedException))]
        [InlineData(OrderStatus.Shipped, typeof(OrderCannotBeShippedTwiceException))]
        public void ShipFailsWithoutCallingShipmentService(OrderStatus initial, Type expectedException)
        {
            var shipmentService = new CountingShipmentService();
            var order = TestOrders.WithStatus(initial);

            Assert.Throws(expectedException, () => order.Ship(shipmentService));
            Assert.Equal(0, shipmentService.Calls);
            Assert.Equal(initial, order.Status);
        }

        [Fact]
        public void StatusTransitionsDoNotChangeAmountsOrItems()
        {
            var order = Order.Create();
            order.AddItem(new Product("salad", 3.56m, Food), 2);
            order.AddItem(new Product("juice", 1.01m, Drinks), 3);

            order.Approve();
            order.Ship(new CountingShipmentService());

            Assert.Equal(OrderStatus.Shipped, order.Status);
            Assert.Equal(2, order.Items.Count);
            Assert.Equal(11.50m, order.Total);
            Assert.Equal(1.35m, order.Tax);
            Assert.Equal("EUR", order.Currency);
        }

        private class CountingShipmentService : IShipmentService
        {
            public int Calls { get; private set; }
            public Order LastOrder { get; private set; }

            public void Ship(Order order)
            {
                Calls++;
                LastOrder = order;
            }
        }
    }
}
