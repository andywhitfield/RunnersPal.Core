using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Controllers;

public class HomeController(ILogger<HomeController> logger) : Controller
{
    public ActionResult Index() => View();

    public ActionResult Error() => View("Error");

    [HttpPost]
    public IActionResult Login()
    {
        var returnPage = Request.Form["return_page"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(returnPage)) returnPage = Url.Content("~/");
        Uri returnPageUri;
        Uri.TryCreate(returnPage, UriKind.RelativeOrAbsolute, out returnPageUri);
        if (returnPageUri == null || returnPageUri.IsAbsoluteUri)
            returnPage = Url.Content("~/");
        HttpContext.Session.SetString("login_returnPage", returnPage);

        return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(LoggingIn)) }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult LoggingIn()
    {
        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        var authId = User?.FindFirstValue("name");
        if (!isAuthenticated || string.IsNullOrWhiteSpace(authId))
            return Redirect("/");

        var userAccount = MassiveDB.Current.FindUser(authId);
        if (userAccount == null)
            userAccount = MassiveDB.Current.CreateUser(authId, HttpContext.Connection.RemoteIpAddress.ToString(), HttpContext.UserDistanceUnits());

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
        return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost]
    public ActionResult UpdateDistanceUnits(int distanceUnit)
    {
        logger.LogInformation("Updating units for user to " + distanceUnit);
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
