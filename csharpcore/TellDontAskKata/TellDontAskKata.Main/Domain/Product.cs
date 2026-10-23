using System;

namespace TellDontAskKata.Main.Domain
{
    public class Product
    {
        public Product(string name, decimal price, Category category)
        {
            Name = name;
            Price = price;
            Category = category ?? throw new ArgumentNullException(nameof(category));
        }

        public string Name { get; }
        public decimal Price { get; }
        public Category Category { get; }

        public decimal UnitaryTax => Money.Round((Price / 100m) * Category.TaxPercentage);

        public decimal UnitaryTaxedAmount => Money.Round(Price + UnitaryTax);
    }
}
