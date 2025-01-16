using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RoutePal;

public class IndexModel(IUserService userService)
    : PageModel
{
    public bool IsLoggedIn => userService.IsLoggedIn;
    
    public void OnGet() { }
}
