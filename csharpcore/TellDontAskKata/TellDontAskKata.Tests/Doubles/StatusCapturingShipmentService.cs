using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Service;

namespace TellDontAskKata.Tests.Doubles
{
    public class StatusCapturingShipmentService : IShipmentService
    {
        private Order _shippedOrder = null;
        private OrderStatus? _statusAtShipment = null;

        public void Ship(Order order)
        {
            _shippedOrder = order;
            _statusAtShipment = order.Status;
        }

        public Order GetShippedOrder()
        {
            return _shippedOrder;
        }

        public OrderStatus? GetStatusAtShipment()
        {
            return _statusAtShipment;
        }
    }
}
