using TellDontAskKata.Main.Domain;

namespace TellDontAskKata.Tests.Builders
{
    public static class TestOrders
    {
        public static Order WithStatus(OrderStatus status, int id = 1)
        {
            return new Order
            {
                Status = status,
                Id = id
            };
        }
    }
}
