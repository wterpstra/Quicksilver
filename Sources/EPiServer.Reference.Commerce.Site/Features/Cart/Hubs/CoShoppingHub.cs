using System;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Reference.Commerce.Site.Features.Cart.Models;
using Mediachase.Commerce.Security;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;

namespace EPiServer.Reference.Commerce.Site.Features.Cart.Hubs
{
    public class CoShoppingHub : Hub
    {
        public override Task OnConnected()
        {
            GuestCartAccessModel guestCartAccess = null;
            if (this.Context.RequestCookies.ContainsKey("GuestCartAccess"))
            {
                var cookie = this.Context.RequestCookies["GuestCartAccess"];
                if (!string.IsNullOrWhiteSpace(cookie?.Value))
                {
                    guestCartAccess = JsonConvert.DeserializeObject<GuestCartAccessModel>(Encoding.UTF8.GetString(Convert.FromBase64String(cookie.Value)));
                }
            }

            var currentUserId = Context.User.GetContactId();

            Groups.Add(Context.ConnectionId, (guestCartAccess?.GuestOfCustomerId ?? currentUserId).ToString());

            return base.OnConnected();
        }
    }
}
