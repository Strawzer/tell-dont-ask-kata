using System;

namespace TellDontAskKata.Main.Domain
{
    internal static class Money
    {
        public static decimal Round(decimal amount)
        {
            return decimal.Round(amount, 2, MidpointRounding.ToPositiveInfinity);
        }
    }
}
