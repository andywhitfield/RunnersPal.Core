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
    [BindProperty] public int? RouteId { get; set; }
    [BindProperty] public string? TimeTaken { get; set; }
    [BindProperty] public string? Comment { get; set; }
    [BindProperty] public string? Cancel { get; set; }

    public IEnumerable<Models.Route> SystemRoutes { get; private set; } = [];

    public async Task OnGet()
        => SystemRoutes = await routeRepository.GetSystemRoutesAsync().ToListAsync();

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrEmpty(Cancel))
            return Redirect("/runlog");

        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        switch (DistanceType)
        {
            case 1:
                if (RouteId == null || RouteId == 0 || Date == null || TimeTaken == null)
                    return BadRequest();

                logger.LogDebug("Getting system route {RouteId}", RouteId);
                var systemRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (systemRoute == null || systemRoute.RouteType != Models.Route.SystemRoute)
                    return BadRequest();

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), systemRoute, TimeTaken, Comment);

                break;
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
