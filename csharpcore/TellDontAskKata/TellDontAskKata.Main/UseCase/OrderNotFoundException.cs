using System;

namespace TellDontAskKata.Main.UseCase
{
    public class OrderNotFoundException : ApplicationException
    {
        public OrderNotFoundException(int orderId)
            : base($"Order {orderId} was not found.")
        {
            OrderId = orderId;
        }

        public int OrderId { get; }
    }
}
