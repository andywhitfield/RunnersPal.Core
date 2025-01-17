using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.RunLog;

public class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)] public DateTime Date { get; set; } = DateTime.Today;
    public string InitialDate => Date.ToString("yyy-MM-dd");

    public void OnGet() { }
}
