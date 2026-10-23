namespace TellDontAskKata.Main.Domain
{
    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public Category Category { get; set; }

        public decimal UnitaryTax => Money.Round((Price / 100m) * Category.TaxPercentage);

        public decimal UnitaryTaxedAmount => Money.Round(Price + UnitaryTax);
    }
}
