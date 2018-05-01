using System;

namespace EPiServer.Reference.Commerce.Site.Features.Cart.Models
{
    public class GuestCartAccessModel
    {
        public string Name { get; set; }
        public Guid GuestOfCustomerId { get; set; }
    }
}