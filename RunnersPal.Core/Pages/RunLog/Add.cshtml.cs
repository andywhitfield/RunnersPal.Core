using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RunLog;

[Authorize]
public class AddModel(ILogger<AddModel> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IRunLogRepository runLogRepository,
    IUserService userService,
    IPaceService paceService)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Date { get; set; }
    [BindProperty] public int DistanceType { get; set; }
    [BindProperty] public decimal? DistanceManual { get; set; }
    [BindProperty] public int? RouteId { get; set; }
    [BindProperty] public string? MapPoints { get; set; }
    [BindProperty] public string? MapName { get; set; }
    [BindProperty] public string? MapNotes { get; set; }
    [BindProperty] public decimal? MapDistance { get; set; }
    [BindProperty] public string? TimeTaken { get; set; }
    [BindProperty] public string? Comment { get; set; }
    [BindProperty] public string? Cancel { get; set; }

    private UserAccount? _userAccount;

    public IEnumerable<Models.Route> SystemRoutes { get; private set; } = [];

    public async Task OnGet()
        => SystemRoutes = await routeRepository.GetSystemRoutesAsync().ToListAsync();

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrEmpty(Cancel))
            return Redirect("/runlog");

        _userAccount = await userAccountRepository.GetUserAccountAsync(User);
        switch (DistanceType)
        {
            case 1:
                if (RouteId == null || RouteId == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return BadRequest();

                logger.LogDebug("Getting system route {RouteId}", RouteId);
                var systemRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (systemRoute == null || systemRoute.RouteType != Models.Route.SystemRoute)
                    return BadRequest();

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(_userAccount, Date.ParseDateTime(), systemRoute, TimeTaken!, Comment);

                break;
            case 2:
                if (DistanceManual == null || DistanceManual == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return BadRequest();

                logger.LogDebug("Creating a new manual distance route for {DistanceManual}km", DistanceManual);
                var manualRoute = await routeRepository.CreateNewRouteAsync(
                    _userAccount,
                    userService.ToUserDistance(DistanceManual ?? 0, _userAccount),
                    "",
                    userService.ToDistanceInMeters(DistanceManual ?? 0, _userAccount),
                    "");

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(_userAccount, Date.ParseDateTime(), manualRoute, TimeTaken!, Comment);

                break;
            case 3:
                if (RouteId == null || RouteId == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return BadRequest();

                logger.LogDebug("Getting user route {RouteId}", RouteId);
                var userRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (userRoute == null || userRoute.RouteType != Models.Route.PrivateRoute || userRoute.Creator != _userAccount.Id)
                    return BadRequest();

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(_userAccount, Date.ParseDateTime(), userRoute, TimeTaken!, Comment);

                break;
            case 4:
                if (MapDistance == null || MapDistance == 0 ||
                    string.IsNullOrEmpty(MapName) ||
                    string.IsNullOrEmpty(MapPoints) ||
                    Date == null ||
                    paceService.TimeTaken(TimeTaken) == null)
                {
                    return BadRequest();
                }

                logger.LogDebug("Creating a new mapped route for {MapName} ({MapNotes}): {MapPoints}", MapName, MapNotes, MapPoints);
                var newUserRoute = await routeRepository.CreateNewRouteAsync(_userAccount, MapName, MapPoints, MapDistance.Value, MapNotes);

                logger.LogDebug("Creating a new run log entry");
                await runLogRepository.CreateNewAsync(_userAccount, Date.ParseDateTime(), newUserRoute, TimeTaken!, Comment);

                break;
            default:
                return BadRequest();
        }
        return Redirect("/runlog");
    }

    public async Task<string> UserUnitsAsync()
        => (Models.DistanceUnits)(_userAccount ??= await userAccountRepository.GetUserAccountAsync(User)).DistanceUnits
            switch { Models.DistanceUnits.Miles => "miles", Models.DistanceUnits.Kilometers => "km", _ => "" };

    public async Task<decimal> UserUnitsMultiplierAsync()
        => (Models.DistanceUnits)(_userAccount ??= await userAccountRepository.GetUserAccountAsync(User)).DistanceUnits
            switch { Models.DistanceUnits.Miles => 1000 * UserService.KilometersToMiles, Models.DistanceUnits.Kilometers => 1000, _ => 1 };
}
