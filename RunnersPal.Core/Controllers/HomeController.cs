using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index() => View();
        public ActionResult Error() => View("Error");

        [HttpPost]
        public IActionResult Login([FromForm] string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return BadRequest();

            var returnPage = Request.Form["return_page"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(returnPage)) returnPage = Url.Content("~/");
            Uri returnPageUri;
            Uri.TryCreate(returnPage, UriKind.RelativeOrAbsolute, out returnPageUri);
            if (returnPageUri == null || returnPageUri.IsAbsoluteUri)
                returnPage = Url.Content("~/");
            HttpContext.Session.SetString("login_returnPage", returnPage);

            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(LoggingIn)) }, provider);
        }

        [HttpGet]
        public IActionResult LoggingIn()
        {
            var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            var openId = User?.Claims?.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (!isAuthenticated || string.IsNullOrWhiteSpace(openId))
                return Redirect("/");

            var userAccount = MassiveDB.Current.FindUser(openId);
            if (userAccount == null)
                userAccount = MassiveDB.Current.CreateUser(openId, HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.UserDistanceUnits());

            HttpContext.Session.Set<long?>("rp_UserAccount", (long?)userAccount.Id);
            HttpContext.Response.Cookies.Append("rp_UserAccount", Secure.EncryptValue(userAccount.Id.ToString()), new CookieOptions { Expires = DateTime.UtcNow.AddYears(1) });

            if (userAccount.UserType == "N")
                return RedirectToAction("FirstTime", "User");

            var returnPage = HttpContext.Session.GetString("login_returnPage");
            if (string.IsNullOrWhiteSpace(returnPage))
                return RedirectToAction("Index", "Home");
            return Redirect(returnPage);
        }

        [HttpGet, HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return SignOut(new AuthenticationProperties { RedirectUri = "/" }, CookieAuthenticationDefaults.AuthenticationScheme);
        }

        [HttpPost]
        public ActionResult UpdateDistanceUnits(int distanceUnit)
        {
            Trace.WriteLine("Updating units for user to " + distanceUnit);
            if (!Enum.IsDefined(typeof(DistanceUnits), distanceUnit))
                return Json(new { Completed = false, Reason = "Invalid distance unit." });

            var newDistanceUnit = (DistanceUnits)distanceUnit;
            HttpContext.Session.Set("rp_UserDistanceUnits", newDistanceUnit);

            var userAccount = HttpContext.UserAccount();
            if (userAccount != null)
            {
                userAccount.DistanceUnits = newDistanceUnit;
                MassiveDB.Current.UpdateUser(userAccount);
            }

            return Json(new { Completed = true });
        }
    }
}
