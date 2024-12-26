using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RunLog;

[Authorize]
public class AddModel(ILogger<AddModel> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IRunLogRepository runLogRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Date { get; set; }
    [BindProperty] public int DistanceType { get; set; }
    [BindProperty] public decimal? DistanceManual { get; set; }
    [BindProperty] public string? TimeTaken { get; set; }
    [BindProperty] public string? Comment { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPost()
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        switch (DistanceType)
        {
            case 2:
                if (DistanceManual == null || DistanceManual == 0 || Date == null || TimeTaken == null)
                    return BadRequest();

                logger.LogDebug("Creating a new manual distance route for {DistanceManual}km", DistanceManual);
                var manualRoute = await routeRepository.CreateNewRouteAsync(userAccount, $"{DistanceManual} km", "", DistanceManual.Value * 1000m, "");

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), manualRoute, TimeTaken, Comment);

                break;
            default:
                return BadRequest();
        }
        return Redirect("/runlog");
    }
}
