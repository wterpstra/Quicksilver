using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using EPiServer.Commerce.Marketing;
using EPiServer.Commerce.Marketing.Promotions;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceApi.Util;
using Mediachase.Commerce;

namespace EPiServer.Reference.Commerce.Site.Features.PromotionApi.Controllers
{
    /// <summary>
    /// Promotion API controller.
    /// </summary>
    [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), RequireHttps, RoutePrefix("episerverapi/commerce/promotion")]
    public class PromotionApiController : ApiController
    {
        private static readonly ApiCallLogger Logger = new ApiCallLogger(typeof(PromotionApiController));
        private readonly IContentRepository _contentRepository;
        private readonly IPromotionEngine _promotionEngine;

        /// <summary>
        /// Initializes a new instance of the PromotionApiController.
        /// </summary>
        /// <param name="contentRepository"></param>
        /// <param name="promotionEngine"></param>
        public PromotionApiController(IContentRepository contentRepository,  IPromotionEngine promotionEngine)
        {
            _contentRepository = contentRepository;
            _promotionEngine = promotionEngine;
        }

        /// <summary>
        /// Add a new coupon code and return back
        /// </summary>
        /// <param name="username">User name</param>
        /// <returns>returns coupon code</returns>
        [AuthorizePermission("EPiServerServiceApi", "WriteAccess"), HttpGet, Route("{username}")]
        [ResponseType(typeof(string))]
        public virtual IHttpActionResult GetCouponCode(string username)
        {
            Logger.LogGet("GetCouponCode", Request, new []{ username});
            
            try
            {
                var campaignReference = CreateCampaigns();
                string promoCode = username;
                if (promoCode.Length > 5)
                {
                    promoCode = promoCode.Substring(0, 5);
                }
                promoCode = $"{promoCode}{GetRandomNumber()}";
                var promotionName = "50 % off Order over $50";
                var minimum = new Money(50m, Currency.USD);
                var promotionId = 
                    CreateSpendAmountGetPercentageDiscountPromotion(campaignReference, promotionName, 50, minimum, promoCode);
                return ContentReference.IsNullOrEmpty(promotionId) ? Ok("") : Ok(promoCode);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message, exception);
                return InternalServerError(exception);
            }
        }

        private ContentReference CreateCampaigns()
        {
            var campaign = _contentRepository.GetDefault<SalesCampaign>(SalesCampaignFolder.CampaignRoot);
            if (campaign != null && !ContentReference.IsNullOrEmpty(campaign.ContentLink))
                return campaign.ContentLink;

            campaign.Name = "QuickSilver";
            campaign.Created = DateTime.UtcNow;
            campaign.IsActive = true;
            campaign.ValidFrom = DateTime.Today;
            campaign.ValidUntil = DateTime.Today.AddMonths(1);
            return _contentRepository.Save(campaign, SaveAction.Publish, AccessLevel.NoAccess);
        }

        private ContentReference CreateSpendAmountGetPercentageDiscountPromotion(ContentReference campaignLink, string promotionName, decimal percentage, Money minimumSpend, string promoCode)
        {
            var promotion = _contentRepository.GetDefault<SpendAmountGetOrderDiscount>(campaignLink);
            promotion.IsActive = true;
            promotion.Name = promotionName;
            promotion.Condition.Amounts = new List<Money>() { minimumSpend };
            promotion.Discount.UseAmounts = false;
            promotion.Discount.Percentage = percentage;
            promotion.Coupon = new CouponData(){Code = promoCode};
            return  _contentRepository.Save(promotion, SaveAction.Publish, AccessLevel.NoAccess);
        }

        private int GetRandomNumber()
        {
            Random rand = new Random(DateTime.Now.Millisecond);
            return  rand.Next(10000, 90000);
        }
    }
}