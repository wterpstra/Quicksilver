using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace PixieEpiServerExtensionCoViewing.Hub
{
    public interface IPixieCoViewingManager
    {
        IHubContext HubContext { get; }
        string StartPresenterSession();
        void SignInAudience(string groupName);
        void SignOut(string groupName);
        void AddToCart(string group, string email, string productId);
    }
}
