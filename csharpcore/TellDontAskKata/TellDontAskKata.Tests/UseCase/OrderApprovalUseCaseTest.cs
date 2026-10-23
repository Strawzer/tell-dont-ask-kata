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
        public void FailedApprovalLeavesOrderStatusUnchanged()
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
            Assert.Equal(OrderStatus.Rejected, initialOrder.Status);
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

        // Characterizes incidental behavior: an unknown order id is not handled explicitly,
        // so the use case dereferences a null order.
        [Theory]
        [Trait("Category", "IncidentalBehavior")]
        [InlineData(true)]
        [InlineData(false)]
        public void UnknownOrderThrowsNullReferenceAndSavesNothing(bool approved)
        {
            var request = new OrderApprovalRequest
            {
                OrderId = 42,
                Approved = approved
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<NullReferenceException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

    }
}
