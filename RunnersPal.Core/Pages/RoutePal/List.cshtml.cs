using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.RoutePal;

[Authorize]
public class ListModel : PageModel
{    
    public Task OnGet() => Task.CompletedTask;
}
