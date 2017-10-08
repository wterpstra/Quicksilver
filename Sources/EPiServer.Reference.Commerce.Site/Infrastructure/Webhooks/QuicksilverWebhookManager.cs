using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EPiServer.ServiceApi.Commerce.Models.Order;
using EPiServer.ServiceLocation;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json;

namespace EPiServer.Reference.Commerce.Site.Infrastructure.Webhooks
{
    [ServiceConfiguration(typeof(IWebHookManager), Lifecycle = ServiceInstanceScope.Singleton)]
    public class QuicksilverWebHookManager : IWebHookManager
    {
        public Task VerifyWebHookAsync(WebHook webHook)
        {
            return new Task(() => { });
        }

        public Task<int> NotifyAsync(string user, IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate)
        {
            return Task.FromResult(default(int));
        }

        public async Task<int> NotifyAllAsync(IEnumerable<NotificationDictionary> notifications, Func<WebHook, string, bool> predicate)
        {
            int i = 0;

            var url = ConfigurationManager.AppSettings["BuyOver50GetCouponEndpoint"];

            foreach (var notification in notifications)
            {
                if (notification.Action != "OrderGroupUpdated" && (Type) notification["OrderGroupType"] != typeof(PurchaseOrder)) continue;
                
                using (var httpClient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(notification);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var result = await httpClient.PostAsync(url, content);

                    if (result.IsSuccessStatusCode) i++;
                }
            }

            return i;
        }
    }
}