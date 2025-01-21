using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RunLog;

public class IndexModel(IUserService userService) : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime Date { get; set; } = DateTime.Today;
    public string InitialDate => Date.ToString("yyy-MM-dd");

    public void OnGet() { }

    public bool IsLoggedIn => userService.IsLoggedIn;
}
