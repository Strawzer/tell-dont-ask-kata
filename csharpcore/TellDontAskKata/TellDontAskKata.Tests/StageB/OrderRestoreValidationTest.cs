using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TellDontAskKata.Main.Domain;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class OrderRestoreValidationTest
    {
        [Theory]
        [InlineData(99)]
        [InlineData(-1)]
        [InlineData(4)]
        public void RestoreRejectsUndefinedStatus(int rawStatus)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => Order.Restore(1, (OrderStatus)rawStatus, "EUR", Enumerable.Empty<OrderItem>()));

            Assert.Equal("status", exception.ParamName);
        }

        [Fact]
        public void RestoreRejectsUndefinedStatusBeforeReadingItems()
        {
            var items = new UnreadableItems();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => Order.Restore(1, (OrderStatus)99, "EUR", items));

            Assert.False(items.WasEnumerated);
        }

        [Theory]
        [InlineData(OrderStatus.Approved)]
        [InlineData(OrderStatus.Rejected)]
        [InlineData(OrderStatus.Shipped)]
        [InlineData(OrderStatus.Created)]
        public void RestoreAcceptsEveryDefinedStatus(OrderStatus status)
        {
            var order = Order.Restore(1, status, "EUR", Enumerable.Empty<OrderItem>());

            Assert.Equal(status, order.Status);
        }

        [Fact]
        public void RestoreAcceptsDefaultStatusWhichIsApproved()
        {
            var order = Order.Restore(1, default(OrderStatus), "EUR", Enumerable.Empty<OrderItem>());

            Assert.Equal(OrderStatus.Approved, order.Status);
        }

        private class UnreadableItems : IEnumerable<OrderItem>
        {
            public bool WasEnumerated { get; private set; }

            public IEnumerator<OrderItem> GetEnumerator()
            {
                WasEnumerated = true;
                return Enumerable.Empty<OrderItem>().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
