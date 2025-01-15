using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RunnersPal.Core.Pages.Calculators;

public class DistanceModel : PageModel
{
    [BindProperty(SupportsGet = true)] public int? Miles { get; set; }
    [BindProperty(SupportsGet = true)] public int? Km { get; set; }

    public void OnGet()
    {
        if (Miles == null || Miles < 0)
            Miles = 0;
        if (Km == null || Km < 0)
            Km = 0;
    }
}
