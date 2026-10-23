using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TellDontAskKata.Main.Service;
using TellDontAskKata.Main.UseCase;

namespace TellDontAskKata.Main.Domain
{
    public class Order
    {
        private readonly List<OrderItem> _items = new List<OrderItem>();
        private readonly ReadOnlyCollection<OrderItem> _readOnlyItems;

        private Order(int id, OrderStatus status, string currency)
        {
            Id = id;
            Status = status;
            Currency = currency;
            _readOnlyItems = _items.AsReadOnly();
        }

        public decimal Total { get; private set; }
        public string Currency { get; }
        public IReadOnlyList<OrderItem> Items => _readOnlyItems;
        public decimal Tax { get; private set; }
        public OrderStatus Status { get; private set; }
        public int Id { get; }

        private bool IsCreated => Status == OrderStatus.Created;
        private bool IsApproved => Status == OrderStatus.Approved;
        private bool IsRejected => Status == OrderStatus.Rejected;
        private bool IsShipped => Status == OrderStatus.Shipped;

        public static Order Create()
        {
            return new Order(0, OrderStatus.Created, "EUR");
        }

        public static Order Restore(int id, OrderStatus status, string currency, IEnumerable<OrderItem> items)
        {
            if (!Enum.IsDefined(typeof(OrderStatus), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Status must be a defined OrderStatus value.");
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var suppliedItems = new List<OrderItem>(items);
            if (suppliedItems.Contains(null))
            {
                throw new ArgumentException("Items must not contain null.", nameof(items));
            }

            var order = new Order(id, status, currency);
            foreach (var item in suppliedItems)
            {
                order.Append(item);
            }

            return order;
        }

        public void AddItem(Product product, int quantity)
        {
            Append(new OrderItem(product, quantity));
        }

        private void Append(OrderItem item)
        {
            _items.Add(item);
            Total += item.TaxedAmount;
            Tax += item.Tax;
        }

        public void Approve()
        {
            if (IsShipped)
            {
                throw new ShippedOrdersCannotBeChangedException();
            }

            if (IsRejected)
            {
                throw new RejectedOrderCannotBeApprovedException();
            }

            Status = OrderStatus.Approved;
        }

        public void Reject()
        {
            if (IsShipped)
            {
                throw new ShippedOrdersCannotBeChangedException();
            }

            if (IsApproved)
            {
                throw new ApprovedOrderCannotBeRejectedException();
            }

            Status = OrderStatus.Rejected;
        }

        public void Ship(IShipmentService shipmentService)
        {
            if (IsCreated || IsRejected)
            {
                throw new OrderCannotBeShippedException();
            }

            if (IsShipped)
            {
                throw new OrderCannotBeShippedTwiceException();
            }

            // Checked after the status guards so invalid states keep reporting their domain exception.
            if (shipmentService == null)
            {
                throw new ArgumentNullException(nameof(shipmentService));
            }

            shipmentService.Ship(this);

            Status = OrderStatus.Shipped;
        }
    }
}
