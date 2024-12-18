using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.RunLog;

public class AddModel : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Date { get; set; }
    
    public Task OnGet() => Task.CompletedTask;
}
