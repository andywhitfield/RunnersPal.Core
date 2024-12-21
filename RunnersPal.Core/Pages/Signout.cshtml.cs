using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages;

public class SignoutModel : PageModel
{
    public Task<IActionResult> OnGet() => Signout();

    public Task<IActionResult> OnPost() => Signout();

    private async Task<IActionResult> Signout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }
}
