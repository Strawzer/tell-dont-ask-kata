using System;
using System.Collections;
using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using Xunit;

namespace TellDontAskKata.Tests.Domain
{
    public class OrderInvariantsTest
    {
        private static readonly Category Food = new Category("food", 10m);
        private static readonly Product Salad = new Product("salad", 3.56m, Food);
        private static readonly Product Tomato = new Product("tomato", 4.65m, Food);

        [Fact]
        public void RestoreCopiesSuppliedItems()
        {
            var saladItem = new OrderItem(Salad, 2);
            var source = new List<OrderItem> { saladItem };

            var order = Order.Restore(1, OrderStatus.Approved, "EUR", source);
            source.Add(new OrderItem(Tomato, 3));
            source.Clear();

            Assert.Single(order.Items);
            Assert.Same(saladItem, order.Items[0]);
            Assert.Equal(7.84m, order.Total);
            Assert.Equal(0.72m, order.Tax);
        }

        [Fact]
        public void RestorePreservesSuppliedCurrency()
        {
            var order = Order.Restore(1, OrderStatus.Created, "USD", new List<OrderItem>());

            Assert.Equal("USD", order.Currency);
        }

        [Fact]
        public void RestoreEnumeratesItemsOnce()
        {
            var items = new OneShotEnumerable(new OrderItem(Salad, 2), new OrderItem(Tomato, 3));

            var order = Order.Restore(1, OrderStatus.Approved, "EUR", items);

            Assert.Equal(1, items.Enumerations);
            Assert.Equal(2, order.Items.Count);
            Assert.Equal(23.20m, order.Total);
            Assert.Equal(2.13m, order.Tax);
        }

        private class OneShotEnumerable : IEnumerable<OrderItem>
        {
            private readonly OrderItem[] _items;

            public OneShotEnumerable(params OrderItem[] items)
            {
                _items = items;
            }

            public int Enumerations { get; private set; }

            public IEnumerator<OrderItem> GetEnumerator()
            {
                Enumerations++;
                if (Enumerations > 1)
                {
                    throw new InvalidOperationException("Items were enumerated more than once.");
                }

                return ((IEnumerable<OrderItem>)_items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
