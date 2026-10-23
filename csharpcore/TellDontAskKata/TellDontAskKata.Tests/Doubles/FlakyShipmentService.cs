using System;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Service;

namespace TellDontAskKata.Tests.Doubles
{
    // Throws on the first failuresBeforeSuccess calls, then succeeds.
    public class FlakyShipmentService : IShipmentService
    {
        private readonly int _failuresBeforeSuccess;

        public FlakyShipmentService(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Calls { get; private set; }
        public int SuccessfulShipments { get; private set; }
        public Order LastShippedOrder { get; private set; }

        public void Ship(Order order)
        {
            Calls++;
            if (Calls <= _failuresBeforeSuccess)
            {
                throw new InvalidOperationException("shipment failed");
            }

            SuccessfulShipments++;
            LastShippedOrder = order;
        }
    }
}
