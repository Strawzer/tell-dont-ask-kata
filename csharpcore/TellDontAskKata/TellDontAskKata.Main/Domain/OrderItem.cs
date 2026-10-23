namespace TellDontAskKata.Main.Domain
{
    public class OrderItem
    {
        public OrderItem(Product product, int quantity)
        {
            Product = product;
            Quantity = quantity;
            TaxedAmount = Money.Round(product.UnitaryTaxedAmount * quantity);
            Tax = Money.Round(product.UnitaryTax * quantity);
        }

        public Product Product { get; }
        public int Quantity { get; }
        public decimal TaxedAmount { get; }
        public decimal Tax { get; }
    }
}
