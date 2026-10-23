using System.Collections.Generic;

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
    }
}
