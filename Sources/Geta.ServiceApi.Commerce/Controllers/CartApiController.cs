using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Order;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Util;
using Geta.ServiceApi.Commerce.Mappings;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;
using OrderGroup = Geta.ServiceApi.Commerce.Models.OrderGroup;

namespace Geta.ServiceApi.Commerce.Controllers
{
    /// <summary>
    /// Cart API controller.
    /// </summary>
    [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), RequireHttps, RoutePrefix("episerverapi/commerce/cart")]
    public class CartApiController : ApiController
    {
        private readonly string _defaultName = Cart.DefaultName;

        private static readonly ApiCallLogger Logger = new ApiCallLogger(typeof(OrderApiController));

        private readonly IOrderRepository _orderRepository;
        private readonly IPromotionEngine _promotionEngine;

        /// <summary>
        /// Initializes a new instance of the CartApiController.
        /// </summary>
        /// <param name="orderRepository"></param>
        /// <param name="promotionEngine"></param>
        public CartApiController(IOrderRepository orderRepository, IPromotionEngine promotionEngine)
        {
            _orderRepository = orderRepository;
            _promotionEngine = promotionEngine;
        }

        /// <summary>
        /// Returns customer's cart.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="name">Cart name - usually "default"</param>
        /// <returns>Customer's cart</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("{customerId}/{name}")]
        [ResponseType(typeof(Cart))]
        public virtual IHttpActionResult GetCart(Guid customerId, string name)
        {
            Logger.LogGet("GetCart", Request, new []{ customerId .ToString(), name});
            if (string.IsNullOrEmpty(name))
            {
                name = _defaultName;
            }

            try
            {
                var cart = _orderRepository.Load<Cart>(customerId, name).FirstOrDefault()
                            ?? _orderRepository.Create<Cart>(customerId, name);
                return Ok(cart);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        /// <summary>
        /// Returns carts.
        /// </summary>
        /// <param name="start">Start record index</param>
        /// <param name="maxCount">Max number of records to return</param>
        /// <returns>Array of carts</returns>
        [AuthorizePermission("EPiServerServiceApi", "ReadAccess"), HttpGet, Route("search/{start}/{maxCount}")]
        [ResponseType(typeof(Cart[]))]
        public virtual IHttpActionResult GetCarts(int start, int maxCount)
        {
            Logger.LogGet("GetCarts", Request, new []{start.ToString(), maxCount.ToString()});

            if (maxCount < 1 || maxCount > 100)
            {
                maxCount = 10;
            }

            Cart[] carts;

            try
            {
                // http://world.episerver.com/documentation/Items/Developers-Guide/EPiServer-Commerce/9/Orders/Searching-for-orders/
                OrderSearchOptions searchOptions = new OrderSearchOptions
                {
                    CacheResults = false,
                    StartingRecord = start,
                    RecordsToRetrieve = maxCount,
                    Namespace = "Mediachase.Commerce.Orders"
                };

                OrderSearchParameters parameters = new OrderSearchParameters();
                searchOptions.Classes.Add("LineItemEx");
                parameters.SqlWhereClause = "OrderGroupId IN (Select ObjectId FROM OrderGroup_ShoppingCart)";
                carts = OrderContext.Current.FindCarts(parameters, searchOptions);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }

            return Ok(carts);
        }

        /// <summary>
        /// Updates existing cart.
        /// </summary>
        /// <param name="cart">Cart model</param>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPut, Route]
        public virtual IHttpActionResult PutCart([FromBody] Cart cart)
        {
            Logger.LogPut("PutCart", Request);

            try
            {
                _orderRepository.Save(cart);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }

            return Ok();
        }

        /// <summary>
        /// Delete cart.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="name">Cart name - usually "default"</param>
        /// <response code="404">Cart not found</response>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpDelete, Route("{customerId}/{name}")]
        public virtual IHttpActionResult DeleteCart(Guid customerId, string name)
        {
            Logger.LogDelete("DeleteCart", Request, new []{customerId.ToString(), name});

            if (string.IsNullOrEmpty(name))
            {
                name = _defaultName;
            }

            var existingCart = _orderRepository.Load<Cart>(customerId, name).FirstOrDefault();

            if (existingCart == null)
            {
                return NotFound();
            }

            try
            {
                _orderRepository.Delete(new OrderReference(existingCart.OrderGroupId, name, customerId, typeof(Cart)));
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }

            return Ok();
        }

        /// <summary>
        /// Creates new cart.
        /// </summary>
        /// <param name="orderGroup">Cart's order group model</param>
        /// <returns>Customer's cart</returns>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpPost, Route]
        [ResponseType(typeof(Cart))]
        public virtual IHttpActionResult PostCart([FromBody] OrderGroup orderGroup)
        {
            Logger.LogPost("PostCart", Request);

            try
            {
                if (string.IsNullOrEmpty(orderGroup.Name))
                {
                    throw new ArgumentNullException(nameof(orderGroup.Name));
                }

                if (orderGroup.CustomerId == Guid.Empty)
                {
                    throw new ArgumentNullException(nameof(orderGroup.CustomerId));
                }

                var cart = _orderRepository.Create<Cart>(orderGroup.CustomerId, orderGroup.Name);

                cart = orderGroup.ConvertToCart(cart);

                _promotionEngine.Run(cart);
                cart.AcceptChanges();

                return Ok(cart);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }
    }
}