using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;
using RunnersPal.Core.ViewModels;

namespace RunnersPal.Core.Controllers;

public class HomeController(ILogger<HomeController> logger, IAuthorisationHandler authorisationHandler) : Controller
{
    public ActionResult Index() => View();

    public ActionResult Error() => View("Error");

    [HttpGet("~/login")]
    public IActionResult Login([FromQuery] string? returnUrl) => View("Login", new LoginViewModel(returnUrl));

    [HttpPost("~/login")]
    [ValidateAntiForgeryToken]
    public IActionResult Login([FromForm] string? returnUrl, [FromForm, Required] string email, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Login", new LoginViewModel(returnUrl));

        var (isReturningUser, verifyOptions) = authorisationHandler.HandleSigninRequest(email, cancellationToken);
        return View("LoginVerify", new LoginVerifyViewModel(returnUrl, email, isReturningUser, verifyOptions));
    }

    [HttpPost("~/login/verify")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginVerify(
        [FromForm] string? returnUrl,
        [FromForm, Required] string email,
        [FromForm, Required] string verifyOptions,
        [FromForm, Required] string verifyResponse,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Redirect("~/login");

        var (isValid, userType) = await authorisationHandler.HandleSigninVerifyRequest(HttpContext, email, verifyOptions, verifyResponse, cancellationToken);
        if (isValid)
        {
            if (userType == "N")
                return RedirectToAction("FirstTime", "User");

            var redirectUri = "~/";
            if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Relative, out var uri))
                redirectUri = uri.ToString();

            return Redirect(redirectUri);
        }
        
        return Redirect("~/login");
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("~/");
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
