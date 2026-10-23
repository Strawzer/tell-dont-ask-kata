using System;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Builders;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.UseCase
{
    public class OrderShipmentUseCaseTest
    {
        private readonly TestOrderRepository _orderRepository;
        private readonly TestShipmentService _shipmentService;
        private readonly OrderShipmentUseCase _useCase;

        public OrderShipmentUseCaseTest()
        {
            _orderRepository = new TestOrderRepository();
            _shipmentService = new TestShipmentService();
            _useCase = new OrderShipmentUseCase(_orderRepository, _shipmentService);
        }


        [Fact]
        public void ShipApprovedOrder()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            _useCase.Run(request);

            Assert.Equal(OrderStatus.Shipped, _orderRepository.GetSavedOrder().Status);
            Assert.Same(initialOrder, _shipmentService.GetShippedOrder());
        }

        [Fact]
        public void CreatedOrdersCannotBeShipped()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Created, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<OrderCannotBeShippedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Null(_shipmentService.GetShippedOrder());
        }

        [Fact]
        public void RejectedOrdersCannotBeShipped()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Rejected, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<OrderCannotBeShippedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Null(_shipmentService.GetShippedOrder());
        }

        [Fact]
        public void ShippedOrdersCannotBeShippedAgain()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Shipped, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<OrderCannotBeShippedTwiceException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Null(_shipmentService.GetShippedOrder());
        }

        [Fact]
        public void SavedOrderIsTheShippedInstance()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            _useCase.Run(request);

            Assert.Same(initialOrder, _orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Shipped, initialOrder.Status);
        }

        [Fact]
        public void ShipmentServiceReceivesOrderWhileStillApproved()
        {
            var capturingShipmentService = new StatusCapturingShipmentService();
            var useCase = new OrderShipmentUseCase(_orderRepository, capturingShipmentService);
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            useCase.Run(request);

            Assert.Same(initialOrder, capturingShipmentService.GetShippedOrder());
            Assert.Equal(OrderStatus.Approved, capturingShipmentService.GetStatusAtShipment());
            Assert.Equal(OrderStatus.Shipped, _orderRepository.GetSavedOrder().Status);
        }

        [Fact]
        public void FailingShipmentServiceLeavesOrderApprovedAndUnsaved()
        {
            var shipmentFailure = new InvalidOperationException("shipment failed");
            var useCase = new OrderShipmentUseCase(_orderRepository, new FailingShipmentService(shipmentFailure));
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 1
            };

            Action actionToTest = () => useCase.Run(request);

            var thrown = Assert.Throws<InvalidOperationException>(actionToTest);
            Assert.Same(shipmentFailure, thrown);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Approved, initialOrder.Status);
        }

        [Fact]
        public void OnlyTheRequestedOrderIsShipped()
        {
            var otherOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            var requestedOrder = TestOrders.WithStatus(OrderStatus.Approved, 2);
            _orderRepository.AddOrder(otherOrder);
            _orderRepository.AddOrder(requestedOrder);

            var request = new OrderShipmentRequest
            {
                OrderId = 2
            };

            _useCase.Run(request);

            Assert.Same(requestedOrder, _shipmentService.GetShippedOrder());
            Assert.Same(requestedOrder, _orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Approved, otherOrder.Status);
        }

        [Fact]
        public void SuccessfulShipmentSavesExactlyOnce()
        {
            _orderRepository.AddOrder(TestOrders.WithStatus(OrderStatus.Approved, 1));

            _useCase.Run(new OrderShipmentRequest { OrderId = 1 });

            Assert.Equal(1, _orderRepository.SaveCount);
        }

        [Theory]
        [InlineData(OrderStatus.Created, typeof(OrderCannotBeShippedException))]
        [InlineData(OrderStatus.Rejected, typeof(OrderCannotBeShippedException))]
        [InlineData(OrderStatus.Shipped, typeof(OrderCannotBeShippedTwiceException))]
        public void FailedShipmentSavesNothing(OrderStatus initial, Type expectedException)
        {
            _orderRepository.AddOrder(TestOrders.WithStatus(initial, 1));

            Action actionToTest = () => _useCase.Run(new OrderShipmentRequest { OrderId = 1 });

            Assert.Throws(expectedException, actionToTest);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        [Fact]
        public void FailingShipmentServiceSavesNothing()
        {
            var useCase = new OrderShipmentUseCase(
                _orderRepository, new FailingShipmentService(new InvalidOperationException("shipment failed")));
            _orderRepository.AddOrder(TestOrders.WithStatus(OrderStatus.Approved, 1));

            Action actionToTest = () => useCase.Run(new OrderShipmentRequest { OrderId = 1 });

            Assert.Throws<InvalidOperationException>(actionToTest);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        // The shipment happens before the save, so a failing save leaves the order shipped
        // externally but not persisted. A retry that reloads the still-Approved order from a real
        // store would ship it a second time (pre-existing risk, unchanged from the original code).
        [Fact]
        public void SaveFailureAfterShipmentPropagatesAndLeavesOrderShipped()
        {
            var saveFailure = new InvalidOperationException("save failed");
            var orderRepository = new ThrowingOnSaveOrderRepository(saveFailure);
            var shipmentService = new FlakyShipmentService(0);
            var useCase = new OrderShipmentUseCase(orderRepository, shipmentService);
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            orderRepository.AddOrder(initialOrder);

            Action actionToTest = () => useCase.Run(new OrderShipmentRequest { OrderId = 1 });

            var thrown = Assert.Throws<InvalidOperationException>(actionToTest);
            Assert.Same(saveFailure, thrown);
            Assert.Equal(1, shipmentService.Calls);
            Assert.Same(initialOrder, shipmentService.LastShippedOrder);
            Assert.Equal(1, orderRepository.SaveAttempts);
            Assert.Equal(OrderStatus.Shipped, initialOrder.Status);
        }

        [Fact]
        public void ShipmentRetryAfterServiceFailureSucceeds()
        {
            var shipmentService = new FlakyShipmentService(1);
            var useCase = new OrderShipmentUseCase(_orderRepository, shipmentService);
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);
            var request = new OrderShipmentRequest { OrderId = 1 };

            Assert.Throws<InvalidOperationException>(() => useCase.Run(request));
            Assert.Equal(OrderStatus.Approved, initialOrder.Status);
            Assert.Equal(0, _orderRepository.SaveCount);

            useCase.Run(request);

            Assert.Equal(2, shipmentService.Calls);
            Assert.Equal(1, shipmentService.SuccessfulShipments);
            Assert.Same(initialOrder, shipmentService.LastShippedOrder);
            Assert.Equal(OrderStatus.Shipped, initialOrder.Status);
            Assert.Same(initialOrder, _orderRepository.GetSavedOrder());
            Assert.Equal(1, _orderRepository.SaveCount);
        }

        // Characterizes incidental behavior: an unknown order id is not handled explicitly,
        // so the use case dereferences a null order.
        [Fact]
        [Trait("Category", "IncidentalBehavior")]
        public void UnknownOrderThrowsNullReferenceAndShipsNothing()
        {
            var request = new OrderShipmentRequest
            {
                OrderId = 42
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<NullReferenceException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Null(_shipmentService.GetShippedOrder());
        }

    }
}
