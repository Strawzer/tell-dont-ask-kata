using System;
using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.Workflow
{
    // Wires the three use cases to one shared InMemoryOrderRepository.
    // Created orders are never assigned an Id, so the created order is addressed as Id 0.
    public class OrderWorkflowTest
    {
        private const int CreatedOrderId = 0;

        private readonly InMemoryOrderRepository _orderRepository;
        private readonly TestShipmentService _shipmentService;
        private readonly OrderCreationUseCase _creation;
        private readonly OrderApprovalUseCase _approval;
        private readonly OrderShipmentUseCase _shipment;

        public OrderWorkflowTest()
        {
            var food = new Category("food", 10m);
            var catalog = new InMemoryProductCatalog(new List<Product>
            {
                new Product("salad", 3.56m, food),
                new Product("tomato", 4.65m, food)
            });

            _orderRepository = new InMemoryOrderRepository();
            _shipmentService = new TestShipmentService();
            _creation = new OrderCreationUseCase(_orderRepository, catalog);
            _approval = new OrderApprovalUseCase(_orderRepository);
            _shipment = new OrderShipmentUseCase(_orderRepository, _shipmentService);
        }

        [Fact]
        public void CreatedOrderIsPersistedUnderIdZero()
        {
            CreateSaladAndTomatoOrder();

            var order = _orderRepository.GetById(CreatedOrderId);
            Assert.NotNull(order);
            Assert.Equal(0, order.Id);
            Assert.Equal(OrderStatus.Created, order.Status);
            Assert.Equal(1, _orderRepository.Count);
        }

        [Fact]
        public void CreateApproveShipSucceedsWithTotalsIntact()
        {
            CreateSaladAndTomatoOrder();

            Approve(true);
            Ship();

            var order = _orderRepository.GetById(CreatedOrderId);
            Assert.Equal(OrderStatus.Shipped, order.Status);
            Assert.Same(order, _shipmentService.GetShippedOrder());
            Assert.Equal("EUR", order.Currency);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(2.13m, order.Tax);
            Assert.Equal(2, order.Items.Count);
            Assert.Equal(3, _orderRepository.SaveCount);
            Assert.Equal(1, _orderRepository.Count);
        }

        [Fact]
        public void CreateRejectShipFails()
        {
            CreateSaladAndTomatoOrder();
            Approve(false);

            Assert.Throws<OrderCannotBeShippedException>(() => Ship());

            Assert.Equal(OrderStatus.Rejected, _orderRepository.GetById(CreatedOrderId).Status);
            Assert.Null(_shipmentService.GetShippedOrder());
            Assert.Equal(2, _orderRepository.SaveCount);
        }

        [Fact]
        public void CreateShipWithoutApprovalFails()
        {
            CreateSaladAndTomatoOrder();

            Assert.Throws<OrderCannotBeShippedException>(() => Ship());

            Assert.Equal(OrderStatus.Created, _orderRepository.GetById(CreatedOrderId).Status);
            Assert.Null(_shipmentService.GetShippedOrder());
            Assert.Equal(1, _orderRepository.SaveCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ShippedOrderCannotBeApprovedOrRejected(bool approved)
        {
            CreateSaladAndTomatoOrder();
            Approve(true);
            Ship();

            Assert.Throws<ShippedOrdersCannotBeChangedException>(() => Approve(approved));

            var order = _orderRepository.GetById(CreatedOrderId);
            Assert.Equal(OrderStatus.Shipped, order.Status);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(3, _orderRepository.SaveCount);
        }

        [Fact]
        public void ShippedOrderCannotBeShippedAgain()
        {
            CreateSaladAndTomatoOrder();
            Approve(true);
            Ship();

            Assert.Throws<OrderCannotBeShippedTwiceException>(() => Ship());

            Assert.Equal(OrderStatus.Shipped, _orderRepository.GetById(CreatedOrderId).Status);
            Assert.Equal(3, _orderRepository.SaveCount);
        }

        private void CreateSaladAndTomatoOrder()
        {
            _creation.Run(new SellItemsRequest
            {
                Requests = new List<SellItemRequest>
                {
                    new SellItemRequest { ProductName = "salad", Quantity = 2 },
                    new SellItemRequest { ProductName = "tomato", Quantity = 3 }
                }
            });
        }

        private void Approve(bool approved)
        {
            _approval.Run(new OrderApprovalRequest { OrderId = CreatedOrderId, Approved = approved });
        }

        private void Ship()
        {
            _shipment.Run(new OrderShipmentRequest { OrderId = CreatedOrderId });
        }
    }
}
