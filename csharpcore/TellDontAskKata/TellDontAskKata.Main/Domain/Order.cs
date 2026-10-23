using System.Collections.Generic;
using TellDontAskKata.Main.Service;
using TellDontAskKata.Main.UseCase;

namespace TellDontAskKata.Main.Domain
{
    public class Order
    {
        private readonly List<OrderItem> _items = new List<OrderItem>();

        public decimal Total { get; set; }
        public string Currency { get; set; }
        public IReadOnlyList<OrderItem> Items => _items;
        public decimal Tax { get; set; }
        public OrderStatus Status { get; set; }
        public int Id { get; set; }

        public static Order Create()
        {
            return new Order
            {
                Status = OrderStatus.Created,
                Currency = "EUR",
                Total = 0m,
                Tax = 0m
            };
        }

        public void AddItem(Product product, int quantity)
        {
            var item = new OrderItem(product, quantity);
            _items.Add(item);
            Total += item.TaxedAmount;
            Tax += item.Tax;
        }

        public void Approve()
        {
            if (Status == OrderStatus.Shipped)
            {
                throw new ShippedOrdersCannotBeChangedException();
            }

            if (Status == OrderStatus.Rejected)
            {
                throw new RejectedOrderCannotBeApprovedException();
            }

            Status = OrderStatus.Approved;
        }

        public void Reject()
        {
            if (Status == OrderStatus.Shipped)
            {
                throw new ShippedOrdersCannotBeChangedException();
            }

            if (Status == OrderStatus.Approved)
            {
                throw new ApprovedOrderCannotBeRejectedException();
            }

            Status = OrderStatus.Rejected;
        }

        public void Ship(IShipmentService shipmentService)
        {
            if (Status == OrderStatus.Created || Status == OrderStatus.Rejected)
            {
                throw new OrderCannotBeShippedException();
            }

            if (Status == OrderStatus.Shipped)
            {
                throw new OrderCannotBeShippedTwiceException();
            }

            shipmentService.Ship(this);

            Status = OrderStatus.Shipped;
        }
    }
}
