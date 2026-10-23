using System;
using System.Collections.Generic;
using System.Linq;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;

namespace TellDontAskKata.Tests.Doubles
{
    public class ThrowingOnSaveOrderRepository : IOrderRepository
    {
        private readonly Exception _exception;
        private readonly IList<Order> _orders = new List<Order>();

        public ThrowingOnSaveOrderRepository(Exception exception)
        {
            _exception = exception;
        }

        public int SaveAttempts { get; private set; }

        public void Save(Order order)
        {
            SaveAttempts++;
            throw _exception;
        }

        public Order GetById(int orderId)
        {
            return _orders.FirstOrDefault(o => o.Id == orderId);
        }

        public void AddOrder(Order order)
        {
            _orders.Add(order);
        }
    }
}
