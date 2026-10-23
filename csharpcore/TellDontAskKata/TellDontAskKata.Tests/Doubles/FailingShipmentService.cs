using System;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Service;

namespace TellDontAskKata.Tests.Doubles
{
    public class FailingShipmentService : IShipmentService
    {
        private readonly Exception _exception;

        public FailingShipmentService(Exception exception)
        {
            _exception = exception;
        }

        public void Ship(Order order)
        {
            throw _exception;
        }
    }
}
