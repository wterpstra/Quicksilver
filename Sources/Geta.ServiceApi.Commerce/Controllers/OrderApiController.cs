using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using EPiServer.Commerce.Order;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Util;
using Geta.ServiceApi.Commerce.Mappings;
using Geta.ServiceApi.Commerce.Models;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using Cart = Mediachase.Commerce.Orders.Cart;
using PurchaseOrder = Mediachase.Commerce.Orders.PurchaseOrder;
using PaymentPlan = Mediachase.Commerce.Orders.PaymentPlan;

namespace Geta.ServiceApi.Commerce.Controllers
{
    /// <summary>
    /// Order API controller.
    /// </summary>
    [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), RequireHttps, RoutePrefix("episerverapi/commerce/order")]
    public class OrderApiController : ApiController
    {
        private static readonly ApiCallLogger Logger = new ApiCallLogger(typeof(OrderApiController));

        private readonly IOrderRepository _orderRepository;

        private readonly string _defaultName = Cart.DefaultName;

        /// <summary>
        /// Initializes a new instance of the OrderApiController.
        /// </summary>
        /// <param name="orderRepository"></param>
        public OrderApiController(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Returns order.
        /// </summary>
        /// <param name="orderGroupId">Order group ID</param>
        /// <returns>Order</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{orderGroupId}")]
        [ResponseType(typeof(Models.PurchaseOrder))]
        public virtual IHttpActionResult GetOrder(int orderGroupId)
        {
            Logger.LogGet("GetOrders", Request, new[] { orderGroupId.ToString() });

            try
            {
                var order = _orderRepository.Load<PurchaseOrder>(orderGroupId);
                if (order == null)
                {
                    return NotFound();
                }

                return Ok(order.ConvertToPurchaseOrder());
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Returns customer's orders.
        /// </summary>
        /// <param name="customerId">Customer ID (GUID)</param>
        /// <returns>Array of orders</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{customerId}/all")]
        [ResponseType(typeof(IEnumerable<IOrderGroup>))]
        public virtual IHttpActionResult GetOrders(Guid customerId)
        {
            Logger.LogGet("GetOrders", Request, new[] { customerId.ToString() });

            try
            {
                var orders = _orderRepository.Load(customerId, _defaultName);
                return Ok(orders);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Searches orders.
        /// </summary>
        /// <param name="start">Start record index</param>
        /// <param name="maxCount">Max number of records to return</param>
        /// <param name="request">Orders search parameters model</param>
        /// <returns>Array of orders</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{start}/{maxCount}/search")]
        [ResponseType(typeof(Models.PurchaseOrder[]))]
        public virtual IHttpActionResult SearchOrders(int start, int maxCount, [FromUri(Name = "")] SearchOrdersRequest request)
        {
            Logger.LogGet("GetOrders", Request, new[] {start.ToString(), maxCount.ToString(), $"{request?.OrderShipmentStatus}", $"{request?.ShippingMethodId}"});

            if (maxCount < 1 || maxCount > 100)
            {
                maxCount = 10;
            }

            try
            {
                var searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = start,
                    RecordsToRetrieve = maxCount,
                    Namespace = "Mediachase.Commerce.Orders"
                };

                var parameters = new OrderSearchParameters();
                searchOptions.Classes.Add("PurchaseOrder");
                parameters.SqlMetaWhereClause = string.Empty;

                if (request?.ModifiedFrom.HasValue ?? false)
                {
                    parameters.SqlMetaWhereClause = $"META.Modified >= '{request.ModifiedFrom.Value:s}'";
                }

                if (request?.OrderShipmentStatus != null && request.ShippingMethodId != null && request.ShippingMethodId != Guid.Empty)
                {
                    parameters.SqlWhereClause =
                        $"[OrderGroupId] IN (SELECT [OrderGroupId] FROM [Shipment] WHERE [Status] = '{request.OrderShipmentStatus}' AND [ShippingMethodId] = '{request.ShippingMethodId}')";
                }
                else if (request?.OrderShipmentStatus != null)
                {
                    parameters.SqlWhereClause = $"[OrderGroupId] IN (SELECT [OrderGroupId] FROM [Shipment] WHERE [Status] = '{request.OrderShipmentStatus}')";
                }
                else if (request?.ShippingMethodId != null && request.ShippingMethodId != Guid.Empty)
                {
                    parameters.SqlWhereClause = $"[OrderGroupId] IN (SELECT [OrderGroupId] FROM [Shipment] WHERE [ShippingMethodId] = '{request.ShippingMethodId}')";
                }

                if (request != null &&  request.Status?.Length > 0)
                {
                    if (!string.IsNullOrEmpty(parameters.SqlWhereClause))
                    {
                        parameters.SqlWhereClause += " AND ";
                    }

                    var statusesParam = string.Join(",", request.Status.Select(x => $"'{x}'"));
                    parameters.SqlWhereClause += $"Status IN ({statusesParam})";
                }

                var orders = OrderContext.Current.FindPurchaseOrders(parameters, searchOptions);
                return Ok(orders.Select(x => x.ConvertToPurchaseOrder()).ToArray());
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Returns orders with tracking number containing 'PO'.
        /// </summary>
        /// <param name="start">Start record index</param>
        /// <param name="maxCount">Max number of records to return</param>
        /// <returns>Array of orders</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{start}/{maxCount}/all")]
        [ResponseType(typeof(Models.PurchaseOrder[]))]
        public virtual IHttpActionResult GetOrders(int start, int maxCount)
        {
            Logger.LogGet("GetOrders", Request, new []{start.ToString(), maxCount.ToString()});

            if (maxCount < 1 || maxCount > 100)
            {
                maxCount = 10;
            }

            try
            {
                // http://world.episerver.com/documentation/Items/Developers-Guide/EPiServer-Commerce/9/Orders/Searching-for-orders/
                var searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = start,
                    RecordsToRetrieve = maxCount,
                    Namespace = "Mediachase.Commerce.Orders"
                };

                var parameters = new OrderSearchParameters();
                searchOptions.Classes.Add("PurchaseOrder");
                parameters.SqlMetaWhereClause = "META.TrackingNumber LIKE '%PO%'";
                parameters.SqlWhereClause = "OrderGroupId IN (SELECT OrdergroupId FROM Shipment WHERE NOT ShipmentTrackingNumber IS NULL)";

                var orders = OrderContext.Current.FindPurchaseOrders(parameters, searchOptions);
                return Ok(orders.Select(x => x.ConvertToPurchaseOrder()).ToArray());
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Updates order.
        /// </summary>
        /// <param name="orderGroupId">Order group ID</param>
        /// <param name="orderGroup">Order group model</param>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPut, Route("{orderGroupId}")]
        public virtual IHttpActionResult PutOrder(int orderGroupId, [FromBody] Models.OrderGroup orderGroup)
        {
            try
            {
                var order = _orderRepository.Load<PurchaseOrder>(orderGroupId);
                order = orderGroup.ConvertToPurchaseOrder(order);
                _orderRepository.Save(order);
                return Ok();
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Deletes order.
        /// </summary>
        /// <param name="orderGroupId">Order group ID</param>
        /// <response code="404">Order not found</response>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpDelete, Route("{orderGroupId}")]
        public virtual IHttpActionResult DeleteOrder(int orderGroupId)
        {
            Logger.LogDelete("DeleteOrder", Request, new[] {orderGroupId.ToString()});

            var existingOrder = _orderRepository.Load<PurchaseOrder>(orderGroupId);

            if (existingOrder == null)
            {
                return NotFound();
            }

            try
            {
                var orderReference = new OrderReference(orderGroupId, existingOrder.Name, existingOrder.CustomerId, typeof (PurchaseOrder));

                _orderRepository.Delete(orderReference);
                return Ok();
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Creates order.
        /// </summary>
        /// <param name="orderGroup">Order group model</param>
        /// <returns>Order</returns>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPost, Route]
        [ResponseType(typeof(Models.PurchaseOrder))]
        public virtual IHttpActionResult PostOrder([FromBody] Models.OrderGroup orderGroup)
        {
            Logger.LogPost("PostOrder", Request);

            try
            {
                var purchaseOrder = _orderRepository.Create<PurchaseOrder>(orderGroup.CustomerId, orderGroup.Name);
                purchaseOrder = orderGroup.ConvertToPurchaseOrder(purchaseOrder);
                _orderRepository.Save(purchaseOrder);

                return Ok(purchaseOrder.ConvertToPurchaseOrder());
            }
            catch(Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Creates payment plan.
        /// </summary>
        /// <param name="orderGroup">Order group model</param>
        /// <returns>Payment plan</returns>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPost, Route("PaymentPlan")]
        [ResponseType(typeof(Models.PaymentPlan))]
        public virtual IHttpActionResult PostPaymentPlan([FromBody] Models.OrderGroup orderGroup)
        {
            Logger.LogPost("PostPaymentPlan", Request);

            try
            {
                var paymentPlan = _orderRepository.Create<PaymentPlan>(orderGroup.CustomerId, orderGroup.Name);
                paymentPlan = orderGroup.ConvertToPaymentPlan(paymentPlan);
                _orderRepository.SaveAsPaymentPlan(paymentPlan);

                return Ok(paymentPlan.ConvertToPaymentPlan());
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }
    }
}