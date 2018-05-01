using System;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Logging;
using EPiServer.Reference.Commerce.Site.Features.Cart.Hubs;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Microsoft.AspNet.SignalR;

namespace EPiServer.Reference.Commerce.Site.Infrastructure
{
    [ServiceConfiguration(typeof(IOrderRepositoryCallback), Lifecycle = ServiceInstanceScope.Singleton)]
    public class SiteOrderRepositoryCallback : IOrderRepositoryCallback
    {
        private readonly ServiceAccessor<IOrderRepository> _orderRepository;
        private readonly ICurrentMarket _currentMarket;

        public SiteOrderRepositoryCallback(ServiceAccessor<IOrderRepository> orderRepository, ICurrentMarket currentMarket)
        {
            _orderRepository = orderRepository;
            _currentMarket = currentMarket;
        }

        private readonly ILogger _logger = LogManager.GetLogger();

        public void OnCreating(Guid customerId, string name)
        {
            _logger.Information($"Creating order: customer [{customerId}], name[{name}].");
        }

        public void OnCreated(OrderReference orderReference)
        {
            _logger.Information($"Created order {orderReference.OrderType}: orderid [{orderReference.OrderGroupId}], customer [{orderReference.CustomerId}], name[{orderReference.Name}].");
        }
        
        public void OnUpdating(OrderReference orderReference)
        {
            _logger.Information($"Updating order {orderReference.OrderType}: orderid [{orderReference.OrderGroupId}], customer [{orderReference.CustomerId}], name[{orderReference.Name}].");
        }

        public void OnUpdated(OrderReference orderReference)
        {
            _logger.Information($"Updated order {orderReference.OrderType}: orderid [{orderReference.OrderGroupId}], customer [{orderReference.CustomerId}], name[{orderReference.Name}].");

            if (orderReference.OrderType == typeof(SerializableCart))
            {
                var cart = _orderRepository().LoadCart<ICart>(orderReference.CustomerId, "Default", _currentMarket);

                var coShoppingHub = GlobalHost.ConnectionManager.GetHubContext<CoShoppingHub>();
                coShoppingHub.Clients.Group(orderReference.CustomerId.ToString()).refreshCart(cart);
            }
        }

        public void OnDeleting(OrderReference orderReference)
        {
            _logger.Information($"Deleting order {orderReference.OrderType}: orderid [{orderReference.OrderGroupId}], customer [{orderReference.CustomerId}], name[{orderReference.Name}].");
        }

        public void OnDeleted(OrderReference orderReference)
        {
            _logger.Information($"Deleted order {orderReference.OrderType}: orderid [{orderReference.OrderGroupId}], customer [{orderReference.CustomerId}], name[{orderReference.Name}].");
        }
    }
}