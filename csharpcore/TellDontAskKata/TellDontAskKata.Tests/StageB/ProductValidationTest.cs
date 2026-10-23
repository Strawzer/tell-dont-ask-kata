using System;
using TellDontAskKata.Main.Domain;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class ProductValidationTest
    {
        [Fact]
        public void ProductRequiresACategory()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => new Product("uncategorized", 1m, null));

            Assert.Equal("category", exception.ParamName);
        }
    }
}
