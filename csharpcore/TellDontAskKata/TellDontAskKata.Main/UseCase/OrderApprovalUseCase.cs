using System;
using TellDontAskKata.Main.Repository;

namespace TellDontAskKata.Main.UseCase
{
    public class OrderApprovalUseCase
    {
        private readonly IOrderRepository _orderRepository;
        public OrderApprovalUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public void Run(OrderApprovalRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var order = _orderRepository.GetById(request.OrderId);

            if (order == null)
            {
                throw new OrderNotFoundException(request.OrderId);
            }

            if (request.Approved)
            {
                order.Approve();
            }
            else
            {
                order.Reject();
            }

            _orderRepository.Save(order);
        }
    }
}
