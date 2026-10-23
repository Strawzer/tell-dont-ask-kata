using System;
using System.Collections.Generic;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;
using TellDontAskKata.Main.UseCase;
using TellDontAskKata.Tests.Doubles;
using Xunit;

namespace TellDontAskKata.Tests.StageB
{
    public class UseCaseInputValidationTest
    {
        private readonly TestOrderRepository _orderRepository = new TestOrderRepository();
        private readonly SpyProductCatalog _productCatalog;
        private readonly OrderCreationUseCase _creationUseCase;

        public UseCaseInputValidationTest()
        {
            var food = new Category("food", 10m);
            _productCatalog = new SpyProductCatalog(new InMemoryProductCatalog(new List<Product>
            {
                new Product("salad", 3.56m, food)
            }));
            _creationUseCase = new OrderCreationUseCase(_orderRepository, _productCatalog);
        }

        [Fact]
        public void CreationWithNullRequestThrowsArgumentNullAndDoesNothing()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => _creationUseCase.Run(null));

            Assert.Equal("request", exception.ParamName);
            Assert.Empty(_productCatalog.RequestedNames);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        [Fact]
        public void CreationWithNullRequestListThrowsArgumentExceptionAndDoesNothing()
        {
            var exception = Assert.Throws<ArgumentException>(
                () => _creationUseCase.Run(new SellItemsRequest { Requests = null }));

            Assert.Equal("request", exception.ParamName);
            Assert.Empty(_productCatalog.RequestedNames);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        [Fact]
        public void CreationWithNullItemThrowsArgumentExceptionBeforeAnyLookup()
        {
            var request = new SellItemsRequest
            {
                Requests = new List<SellItemRequest>
                {
                    new SellItemRequest { ProductName = "salad", Quantity = 1 },
                    null
                }
            };

            var exception = Assert.Throws<ArgumentException>(() => _creationUseCase.Run(request));

            Assert.Equal("request", exception.ParamName);
            Assert.Empty(_productCatalog.RequestedNames);
            Assert.Equal(0, _orderRepository.SaveCount);
        }

        [Fact]
        public void ApprovalWithNullRequestThrowsArgumentNullBeforeLoadingOrder()
        {
            var repository = new GetByIdCountingRepository();
            var useCase = new OrderApprovalUseCase(repository);

            var exception = Assert.Throws<ArgumentNullException>(() => useCase.Run(null));

            Assert.Equal("request", exception.ParamName);
            Assert.Equal(0, repository.GetByIdCalls);
            Assert.Equal(0, repository.SaveCalls);
        }

        [Fact]
        public void ShipmentWithNullRequestThrowsArgumentNullBeforeLoadingOrder()
        {
            var repository = new GetByIdCountingRepository();
            var shipmentService = new TestShipmentService();
            var useCase = new OrderShipmentUseCase(repository, shipmentService);

            var exception = Assert.Throws<ArgumentNullException>(() => useCase.Run(null));

            Assert.Equal("request", exception.ParamName);
            Assert.Equal(0, repository.GetByIdCalls);
            Assert.Equal(0, repository.SaveCalls);
            Assert.Null(shipmentService.GetShippedOrder());
        }

        private class GetByIdCountingRepository : IOrderRepository
        {
            public int GetByIdCalls { get; private set; }
            public int SaveCalls { get; private set; }

            public Order GetById(int orderId)
            {
                GetByIdCalls++;
                return null;
            }

            public void Save(Order order)
            {
                SaveCalls++;
            }
        }
    }
}
