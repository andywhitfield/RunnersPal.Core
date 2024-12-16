using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages;

public class IndexModel : PageModel
{
    public Task OnGet()
    {
        return Task.CompletedTask;
    }
}
