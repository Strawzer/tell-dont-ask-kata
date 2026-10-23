using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;

namespace TellDontAskKata.Tests.Doubles
{
    // Stores orders by Id and hands back the same instances (no copies).
    // OrderCreationUseCase never assigns an Id, so every created order is stored under Id 0
    // and saving a second created order replaces the first: use one created order per repository.
    public class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Dictionary<int, Order> _orders = new Dictionary<int, Order>();

        public int SaveCount { get; private set; }

        public int Count => _orders.Count;

        public void Save(Order order)
        {
            _orders[order.Id] = order;
            SaveCount++;
        }

        public Order GetById(int orderId)
        {
            return _orders.TryGetValue(orderId, out var order) ? order : null;
        }
    }
}
