using TellDontAskKata.Main.Domain;
using Xunit;

namespace TellDontAskKata.Tests.Domain
{
    public class OrderReviewFixesTest
    {
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
    }
}
