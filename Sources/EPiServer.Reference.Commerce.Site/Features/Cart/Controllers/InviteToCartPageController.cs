using System;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using EPiServer.Reference.Commerce.Site.Features.Cart.Models;
using EPiServer.Reference.Commerce.Site.Features.Cart.Pages;
using EPiServer.Reference.Commerce.Site.Features.Cart.ViewModels;
using EPiServer.Reference.Commerce.Site.Infrastructure.Facades;
using EPiServer.Web.Mvc;
using EPiServer.Web.Mvc.Html;
using Newtonsoft.Json;

namespace EPiServer.Reference.Commerce.Site.Features.Cart.Controllers
{
    public class InviteToCartPageController : PageController<InviteToCartPage>
    {
        private readonly CustomerContextFacade _customerContext;

        public InviteToCartPageController(CustomerContextFacade customerContext)
        {
            _customerContext = customerContext;
        }

        [HttpGet]
        public ActionResult Index(InviteToCartPage currentPage)
        {
            var viewModel = new InviteToCartPageViewModel
            {
                CurrentPage = currentPage,
                CustomerId = _customerContext.CurrentContactId
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Index(InviteToCartPage currentPage, string name, string email)
        {
            var relativeUrl = Url.ContentUrl(currentPage.ContentLink);
            var absoluteUrl = new Uri(Request.Url, relativeUrl);

            var fullUrl = $"{absoluteUrl}/Join?name={name}&guestOfCustomerId={_customerContext.CurrentContactId}";

            using (var smtpClient = new SmtpClient())
            using (var mail = new MailMessage())
            {
                mail.To.Add(email);
                mail.Subject = "Cart invite";
                mail.Body = fullUrl;

                smtpClient.Send(mail);
            }

            return Redirect("/");
        }
        
        public ActionResult Join(string name, string guestOfCustomerId)
        {
            var cookie = new HttpCookie("GuestCartAccess");

            var guestCartAccess = new GuestCartAccessModel
            {
                Name = name,
                GuestOfCustomerId = Guid.Parse(guestOfCustomerId)
            };

            cookie.Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(guestCartAccess)));

            Response.Cookies.Remove("GuestCartAcess");
            Response.Cookies.Add(cookie);

            return Redirect("/");
        }

        public ActionResult Leave()
        {
            var cookie = Request.Cookies["GuestCartAccess"];
            if (cookie == null) return Redirect("/");

            cookie.Expires = DateTime.Now.AddDays(-1);

            Response.Cookies.Remove("GuestCartAccess");
            Response.Cookies.Add(cookie);

            return Redirect("/");
        }
    }
}