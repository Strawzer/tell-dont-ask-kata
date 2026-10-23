using System;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class OrderNotFoundTest
    {
        private readonly TestOrderRepository _orderRepository = new TestOrderRepository();
        private readonly TestShipmentService _shipmentService = new TestShipmentService();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ApprovalOfUnknownOrderThrowsOrderNotFoundCarryingTheId(bool approved)
        {
            var useCase = new OrderApprovalUseCase(_orderRepository);

            var exception = Assert.Throws<OrderNotFoundException>(
                () => useCase.Run(new OrderApprovalRequest { OrderId = 42, Approved = approved }));

            Assert.Equal(42, exception.OrderId);
            Assert.Contains("42", exception.Message);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        [Fact]
        public void ShipmentOfUnknownOrderThrowsOrderNotFoundCarryingTheId()
        {
            var useCase = new OrderShipmentUseCase(_orderRepository, _shipmentService);

            var exception = Assert.Throws<OrderNotFoundException>(
                () => useCase.Run(new OrderShipmentRequest { OrderId = 42 }));

            Assert.Equal(42, exception.OrderId);
            Assert.Contains("42", exception.Message);
            Assert.Equal(0, _orderRepository.SaveCount);
            Assert.Null(_shipmentService.GetShippedOrder());
        }

        [Fact]
        public void OrderNotFoundIsAnApplicationExceptionLikeTheOtherUseCaseExceptions()
        {
            var exception = new OrderNotFoundException(7);

            Assert.IsAssignableFrom<ApplicationException>(exception);
            Assert.Equal(7, exception.OrderId);
        }
    }
}
