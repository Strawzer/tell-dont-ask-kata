using System;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Builders;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.UseCase
{
    public class OrderApprovalUseCaseTest
    {
        private readonly TestOrderRepository _orderRepository;
        private readonly OrderApprovalUseCase _useCase;

        public OrderApprovalUseCaseTest()
        {
            _orderRepository = new TestOrderRepository();
            _useCase = new OrderApprovalUseCase(_orderRepository);
        }


        [Fact]
        public void ApprovedExistingOrder()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Created, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = true
            };

            _useCase.Run(request);

            var savedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(OrderStatus.Approved, savedOrder.Status);
        }

        [Fact]
        public void RejectedExistingOrder()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Created, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = false
            };

            _useCase.Run(request);

            var savedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(OrderStatus.Rejected, savedOrder.Status);
        }


        [Fact]
        public void CannotApproveRejectedOrder()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Rejected, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = true
            };


            Action actionToTest = () => _useCase.Run(request);
      
            Assert.Throws<RejectedOrderCannotBeApprovedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Rejected, initialOrder.Status);
        }

        [Fact]
        public void CannotRejectApprovedOrder()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = false
            };


            Action actionToTest = () => _useCase.Run(request);
            
            Assert.Throws<ApprovedOrderCannotBeRejectedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

        [Fact]
        public void ShippedOrdersCannotBeRejected()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Shipped, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = false
            };


            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<ShippedOrdersCannotBeChangedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

        [Fact]
        public void ShippedOrdersCannotBeApproved()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Shipped, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = true
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<ShippedOrdersCannotBeChangedException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Shipped, initialOrder.Status);
        }

        [Fact]
        public void ApprovingAlreadyApprovedOrderIsAllowed()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Approved, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = true
            };

            _useCase.Run(request);

            var savedOrder = _orderRepository.GetSavedOrder();
            Assert.Same(initialOrder, savedOrder);
            Assert.Equal(OrderStatus.Approved, savedOrder.Status);
        }

        [Fact]
        public void RejectingAlreadyRejectedOrderIsAllowed()
        {
            var initialOrder = TestOrders.WithStatus(OrderStatus.Rejected, 1);
            _orderRepository.AddOrder(initialOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 1,
                Approved = false
            };

            _useCase.Run(request);

            var savedOrder = _orderRepository.GetSavedOrder();
            Assert.Same(initialOrder, savedOrder);
            Assert.Equal(OrderStatus.Rejected, savedOrder.Status);
        }

        [Fact]
        public void OnlyTheRequestedOrderIsChanged()
        {
            var otherOrder = TestOrders.WithStatus(OrderStatus.Created, 1);
            var requestedOrder = TestOrders.WithStatus(OrderStatus.Created, 2);
            _orderRepository.AddOrder(otherOrder);
            _orderRepository.AddOrder(requestedOrder);

            var request = new OrderApprovalRequest
            {
                OrderId = 2,
                Approved = true
            };

            _useCase.Run(request);

            Assert.Same(requestedOrder, _orderRepository.GetSavedOrder());
            Assert.Equal(OrderStatus.Approved, requestedOrder.Status);
            Assert.Equal(OrderStatus.Created, otherOrder.Status);
        }

        [Theory]
        [InlineData(OrderStatus.Created, true)]
        [InlineData(OrderStatus.Created, false)]
        [InlineData(OrderStatus.Approved, true)]
        [InlineData(OrderStatus.Rejected, false)]
        public void SuccessfulApprovalOrRejectionSavesExactlyOnce(OrderStatus initial, bool approved)
        {
            _orderRepository.AddOrder(TestOrders.WithStatus(initial, 1));

            _useCase.Run(new OrderApprovalRequest { OrderId = 1, Approved = approved });

            Assert.Equal(1, _orderRepository.SaveCount);
        }

        [Theory]
        [InlineData(OrderStatus.Rejected, true, typeof(RejectedOrderCannotBeApprovedException))]
        [InlineData(OrderStatus.Approved, false, typeof(ApprovedOrderCannotBeRejectedException))]
        [InlineData(OrderStatus.Shipped, true, typeof(ShippedOrdersCannotBeChangedException))]
        [InlineData(OrderStatus.Shipped, false, typeof(ShippedOrdersCannotBeChangedException))]
        public void FailedApprovalOrRejectionSavesNothing(OrderStatus initial, bool approved, Type expectedException)
        {
            _orderRepository.AddOrder(TestOrders.WithStatus(initial, 1));

            Action actionToTest = () => _useCase.Run(new OrderApprovalRequest { OrderId = 1, Approved = approved });

            Assert.Throws(expectedException, actionToTest);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        // Characterizes incidental behavior: status checks only exclude known states,
        // so an order with an undefined status value can be approved or rejected.
        [Theory]
        [Trait("Category", "IncidentalBehavior")]
        [InlineData(true, OrderStatus.Approved)]
        [InlineData(false, OrderStatus.Rejected)]
        public void OrderWithUndefinedStatusCanBeApprovedOrRejected(bool approved, OrderStatus expected)
        {
            var initialOrder = TestOrders.WithStatus((OrderStatus)99, 1);
            _orderRepository.AddOrder(initialOrder);

            _useCase.Run(new OrderApprovalRequest { OrderId = 1, Approved = approved });

            Assert.Same(initialOrder, _orderRepository.GetSavedOrder());
            Assert.Equal(expected, initialOrder.Status);
            Assert.Equal(1, _orderRepository.SaveCount);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnknownOrderThrowsOrderNotFoundAndSavesNothing(bool approved)
        {
            var request = new OrderApprovalRequest
            {
                OrderId = 42,
                Approved = approved
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<OrderNotFoundException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

    }
}
