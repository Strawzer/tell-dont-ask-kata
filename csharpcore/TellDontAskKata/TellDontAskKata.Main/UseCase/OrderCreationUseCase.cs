using System;
using TellDontAskKata.Main.Domain;
using TellDontAskKata.Main.Repository;

namespace TellDontAskKata.Main.UseCase
{
    public class OrderCreationUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductCatalog _productCatalog;

        public OrderCreationUseCase(
            IOrderRepository orderRepository,
            IProductCatalog productCatalog)
        {
            _orderRepository = orderRepository;
            _productCatalog = productCatalog;
        }

        public void Run(SellItemsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Requests == null)
            {
                throw new ArgumentException("Requests must not be null.", nameof(request));
            }

            if (request.Requests.Contains(null))
            {
                throw new ArgumentException("Requests must not contain null.", nameof(request));
            }

            var order = Order.Create();

            foreach (var itemRequest in request.Requests)
            {
                var product = _productCatalog.GetByName(itemRequest.ProductName);

                if (product == null)
                {
                    throw new UnknownProductException();
                }

                order.AddItem(product, itemRequest.Quantity);
            }

            _orderRepository.Save(order);
        }
    }
}
