using System;
using EPiServer.Reference.Commerce.Site.Features.Cart.Pages;

namespace EPiServer.Reference.Commerce.Site.Features.Cart.ViewModels
{
    public class InviteToCartPageViewModel
    {
        public InviteToCartPage CurrentPage { get; set; }
        public Guid CustomerId { get; set; }
    }
}