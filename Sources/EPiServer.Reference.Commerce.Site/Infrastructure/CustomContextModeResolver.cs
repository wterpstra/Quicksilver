using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Internal;

namespace EPiServer.Reference.Commerce.Site.Infrastructure
{
    [ServiceConfiguration(IncludeServiceAccessor = true)]
    [ServiceConfiguration(typeof(ContextModeResolver), IncludeServiceAccessor = true)]
    [ServiceConfiguration(typeof(IContextModeResolver), IncludeServiceAccessor = true)]
    public class CustomContextModeResolver : ContextModeResolver
    {
        public override ContextMode CurrentMode()
        {
            if (HttpContext.Current?.Request?.Cookies["GuestCartAccess"] != null)
            {
                return ContextMode.Undefined;
            }

            return base.CurrentMode();
        }
    }
}