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
