using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.User;

[Authorize]
public class ProfileModel : PageModel
{
    public Task OnGet() => Task.CompletedTask;
}
