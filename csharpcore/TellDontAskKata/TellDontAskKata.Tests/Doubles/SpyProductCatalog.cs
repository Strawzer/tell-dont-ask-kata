using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;

namespace TellDontAskKata.Tests.Doubles
{
    public class SpyProductCatalog : IProductCatalog
    {
        private readonly IProductCatalog _inner;
        private readonly List<string> _requestedNames = new List<string>();

        public SpyProductCatalog(IProductCatalog inner)
        {
            _inner = inner;
        }

        public IReadOnlyList<string> RequestedNames => _requestedNames;

        public Product GetByName(string name)
        {
            _requestedNames.Add(name);
            return _inner.GetByName(name);
        }
    }
}
