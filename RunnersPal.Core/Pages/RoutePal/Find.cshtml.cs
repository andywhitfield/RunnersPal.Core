using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.RoutePal;

[Authorize]
public class FindModel : PageModel
{    
    public Task OnGet() => Task.CompletedTask;
}
