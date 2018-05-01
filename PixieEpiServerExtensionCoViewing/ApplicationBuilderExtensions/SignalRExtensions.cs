using System.Configuration;
using Microsoft.AspNet.SignalR;
using Owin;

namespace PixieEpiServerExtensionCoViewing.ApplicationBuilderExtensions
{
    public static class SignalRExtensions
    {
        public static void SignalRStartUp(this IAppBuilder app)
        {
            if (ConfigurationManager.ConnectionStrings["Microsoft.ServiceBus.ConnectionString"] != null)
            {
                var serviceBusConnectionString = ConfigurationManager.ConnectionStrings["Microsoft.ServiceBus.ConnectionString"].ConnectionString;

                if (!string.IsNullOrWhiteSpace(serviceBusConnectionString))
                {
                    GlobalHost.DependencyResolver.UseServiceBus(new ServiceBusScaleoutConfiguration(serviceBusConnectionString, "PixieEpiServerExtensionCoViewing"));
                }
            }

            //ToDo:Presence monitoring

            app.MapSignalR();
            GlobalHost.Configuration.MaxIncomingWebSocketMessageSize = null;
        }
    }
}
