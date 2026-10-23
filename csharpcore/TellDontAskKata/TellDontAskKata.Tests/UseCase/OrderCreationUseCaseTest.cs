using System;
using System.Collections.Generic;
using System.Globalization;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.UseCase
{
    public class OrderCreationUseCaseTest
    {
        private readonly TestOrderRepository _orderRepository;
        private readonly IProductCatalog _productCatalog;
        private readonly OrderCreationUseCase _useCase;

        public OrderCreationUseCaseTest()
        {
            var food = new Category { 
                Name = "food",
                TaxPercentage = 10m
            };

            _productCatalog = new InMemoryProductCatalog(new List<Product>
            {
                new Product
                {
                    Name = "salad",
                    Price = 3.56m,
                    Category = food
                },
                new Product
                {
                    Name = "tomato",
                    Price = 4.65m,
                    Category = food
                }
            });

            _orderRepository = new TestOrderRepository();

            _useCase = new OrderCreationUseCase(_orderRepository, _productCatalog);
        }


        [Fact]
        public void SellMultipleItems()
        {
            var saladRequest = new SellItemRequest
            {
                ProductName = "salad",
                Quantity = 2
            };

            var tomatoRequest = new SellItemRequest
            {
                ProductName = "tomato",
                Quantity = 3
            };

            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> { saladRequest, tomatoRequest }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(OrderStatus.Created, insertedOrder.Status);
            Assert.Equal(23.20m, insertedOrder.Total);
            Assert.Equal(2.13m, insertedOrder.Tax);
            Assert.Equal("EUR", insertedOrder.Currency);
            Assert.Equal(2, insertedOrder.Items.Count);
            Assert.Equal("salad", insertedOrder.Items[0].Product.Name);
            Assert.Equal(3.56m, insertedOrder.Items[0].Product.Price);
            Assert.Equal(2, insertedOrder.Items[0].Quantity);
            Assert.Equal(7.84m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(0.72m, insertedOrder.Items[0].Tax);
            Assert.Equal("tomato", insertedOrder.Items[1].Product.Name);
            Assert.Equal(4.65m, insertedOrder.Items[1].Product.Price);
            Assert.Equal(3, insertedOrder.Items[1].Quantity);
            Assert.Equal(15.36m, insertedOrder.Items[1].TaxedAmount);
            Assert.Equal(1.41m, insertedOrder.Items[1].Tax);
        }

        [Fact]
        public void UnknownProduct()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> { 
                    new SellItemRequest { ProductName = "unknown product"}
                }
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<UnknownProductException>(actionToTest);
        }

        [Fact]
        public void UnknownProductSavesNothing()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "unknown product", Quantity = 1 }
                }
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<UnknownProductException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

        [Fact]
        public void UnknownProductAfterValidProductsSavesNothing()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = 2 },
                    new SellItemRequest { ProductName = "tomato", Quantity = 3 },
                    new SellItemRequest { ProductName = "unknown product", Quantity = 1 }
                }
            };

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<UnknownProductException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

        [Fact]
        public void SellSingleItem()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = 1 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(OrderStatus.Created, insertedOrder.Status);
            Assert.Equal("EUR", insertedOrder.Currency);
            Assert.Equal(3.92m, insertedOrder.Total);
            Assert.Equal(0.36m, insertedOrder.Tax);
            Assert.Single(insertedOrder.Items);
            Assert.Equal("salad", insertedOrder.Items[0].Product.Name);
            Assert.Equal(1, insertedOrder.Items[0].Quantity);
            Assert.Equal(3.92m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(0.36m, insertedOrder.Items[0].Tax);
        }

        [Fact]
        public void EmptyRequestSavesEmptyCreatedOrder()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest>()
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.NotNull(insertedOrder);
            Assert.Equal(OrderStatus.Created, insertedOrder.Status);
            Assert.Equal("EUR", insertedOrder.Currency);
            Assert.Equal(0m, insertedOrder.Total);
            Assert.Equal(0m, insertedOrder.Tax);
            Assert.NotNull(insertedOrder.Items);
            Assert.Empty(insertedOrder.Items);
        }

        [Fact]
        public void ItemsPreserveRequestOrder()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "tomato", Quantity = 1 },
                    new SellItemRequest { ProductName = "salad", Quantity = 1 },
                    new SellItemRequest { ProductName = "tomato", Quantity = 2 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(3, insertedOrder.Items.Count);
            Assert.Equal("tomato", insertedOrder.Items[0].Product.Name);
            Assert.Equal(1, insertedOrder.Items[0].Quantity);
            Assert.Equal("salad", insertedOrder.Items[1].Product.Name);
            Assert.Equal(1, insertedOrder.Items[1].Quantity);
            Assert.Equal("tomato", insertedOrder.Items[2].Product.Name);
            Assert.Equal(2, insertedOrder.Items[2].Quantity);
        }

        [Fact]
        public void SameProductRequestedTwiceProducesTwoSeparateItems()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = 1 },
                    new SellItemRequest { ProductName = "salad", Quantity = 2 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(2, insertedOrder.Items.Count);
            Assert.Equal(3.92m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(7.84m, insertedOrder.Items[1].TaxedAmount);
            Assert.Equal(11.76m, insertedOrder.Total);
            Assert.Equal(1.08m, insertedOrder.Tax);
        }

        [Fact]
        public void ItemReferencesProductReturnedByCatalog()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = 1 },
                    new SellItemRequest { ProductName = "tomato", Quantity = 1 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Same(_productCatalog.GetByName("salad"), insertedOrder.Items[0].Product);
            Assert.Same(_productCatalog.GetByName("tomato"), insertedOrder.Items[1].Product);
        }

        [Fact]
        public void ZeroQuantityProducesItemWithZeroAmounts()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = 0 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Single(insertedOrder.Items);
            Assert.Equal(0, insertedOrder.Items[0].Quantity);
            Assert.Equal(0m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(0m, insertedOrder.Items[0].Tax);
            Assert.Equal(0m, insertedOrder.Total);
            Assert.Equal(0m, insertedOrder.Tax);
        }

        // Characterizes incidental behavior: negative quantities are not validated.
        [Fact]
        [Trait("Category", "IncidentalBehavior")]
        public void NegativeQuantityProducesNegativeAmounts()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "salad", Quantity = -1 }
                }
            };

            _useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(-3.92m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(-0.36m, insertedOrder.Items[0].Tax);
            Assert.Equal(-3.92m, insertedOrder.Total);
            Assert.Equal(-0.36m, insertedOrder.Tax);
        }

        // Characterizes incidental behavior: a request without a Requests list is not validated.
        [Fact]
        [Trait("Category", "IncidentalBehavior")]
        public void NullRequestListThrowsNullReferenceAndSavesNothing()
        {
            var request = new SellItemsRequest();

            Action actionToTest = () => _useCase.Run(request);

            Assert.Throws<NullReferenceException>(actionToTest);
            Assert.Null(_orderRepository.GetSavedOrder());
        }

        [Theory]
        // 1.01 * 10% = 0.101 -> 0.11 (AwayFromZero and ToEven would give 0.10)
        [InlineData("1.01", "10", 1, "0.11", "1.12")]
        [InlineData("1.01", "10", 3, "0.33", "3.36")]
        // 0.333 * 10% = 0.0333 -> 0.04; 0.333 + 0.04 = 0.373 -> 0.38; rounded per unit before multiplying
        [InlineData("0.333", "10", 3, "0.12", "1.14")]
        // 1.001 with no tax -> 1.01
        [InlineData("1.001", "0", 1, "0", "1.01")]
        // 2.50 * 20% = 0.50 exactly; no rounding needed
        [InlineData("2.50", "20", 2, "1.00", "6.00")]
        public void AmountsAreRoundedUpPerUnitToTwoDecimals(
            string price, string taxPercentage, int quantity, string expectedTaxText, string expectedTaxedAmountText)
        {
            var expectedTax = decimal.Parse(expectedTaxText, CultureInfo.InvariantCulture);
            var expectedTaxedAmount = decimal.Parse(expectedTaxedAmountText, CultureInfo.InvariantCulture);
            var useCase = CreateUseCaseWithSingleProduct(
                "item",
                decimal.Parse(price, CultureInfo.InvariantCulture),
                decimal.Parse(taxPercentage, CultureInfo.InvariantCulture));
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "item", Quantity = quantity }
                }
            };

            useCase.Run(request);

            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(expectedTax, insertedOrder.Items[0].Tax);
            Assert.Equal(expectedTaxedAmount, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(expectedTax, insertedOrder.Tax);
            Assert.Equal(expectedTaxedAmount, insertedOrder.Total);
        }

        [Fact]
        public void TotalsAreSumsOfLineAmountsAcrossTaxCategories()
        {
            var books = new Category { Name = "books", TaxPercentage = 0m };
            var drinks = new Category { Name = "drinks", TaxPercentage = 20m };
            var catalog = new InMemoryProductCatalog(new List<Product>
            {
                new Product { Name = "novel", Price = 12.49m, Category = books },
                new Product { Name = "juice", Price = 1.01m, Category = drinks }
            });
            var useCase = new OrderCreationUseCase(_orderRepository, catalog);
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest> {
                    new SellItemRequest { ProductName = "novel", Quantity = 2 },
                    new SellItemRequest { ProductName = "juice", Quantity = 3 }
                }
            };

            useCase.Run(request);

            // juice: 1.01 * 20% = 0.202 -> 0.21; 1.01 + 0.21 = 1.22
            Order insertedOrder = _orderRepository.GetSavedOrder();
            Assert.Equal(24.98m, insertedOrder.Items[0].TaxedAmount);
            Assert.Equal(0m, insertedOrder.Items[0].Tax);
            Assert.Equal(3.66m, insertedOrder.Items[1].TaxedAmount);
            Assert.Equal(0.63m, insertedOrder.Items[1].Tax);
            Assert.Equal(28.64m, insertedOrder.Total);
            Assert.Equal(0.63m, insertedOrder.Tax);
        }

        private OrderCreationUseCase CreateUseCaseWithSingleProduct(string name, decimal price, decimal taxPercentage)
        {
            var category = new Category { Name = "category", TaxPercentage = taxPercentage };
            var catalog = new InMemoryProductCatalog(new List<Product>
            {
                new Product { Name = name, Price = price, Category = category }
            });
            return new OrderCreationUseCase(_orderRepository, catalog);
        }

    }
}
