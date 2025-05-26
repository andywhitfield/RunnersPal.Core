using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RunLog;

[Authorize]
public class ActivityModel(ILogger<ActivityModel> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IRunLogRepository runLogRepository,
    IUserService userService,
    IPaceService paceService)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int? ActivityId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Date { get; set; }
    [BindProperty] public int DistanceType { get; set; }
    [BindProperty] public decimal? DistanceManual { get; set; }
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }
    [BindProperty] public string? MapPoints { get; set; }
    [BindProperty] public string? MapName { get; set; }
    [BindProperty] public string? MapNotes { get; set; }
    [BindProperty] public decimal? MapDistance { get; set; }
    [BindProperty] public string? TimeTaken { get; set; }
    [BindProperty] public string? Comment { get; set; }
    [BindProperty] public string? Add { get; set; }
    [BindProperty] public string? Save { get; set; }
    [BindProperty] public string? Delete { get; set; }
    [BindProperty] public string? Cancel { get; set; }

    private UserAccount? _userAccount;

    public IEnumerable<Models.Route> SystemRoutes { get; private set; } = [];

    public async Task OnGet()
    {
        SystemRoutes = await routeRepository.GetSystemRoutesAsync().ToListAsync();

        _userAccount = await userAccountRepository.GetUserAccountAsync(User);
        if (ActivityId == null)
        {
            Date = (DateTime.TryParse(Date, out var dt) ? dt : DateTime.Today).ToString("yyyy-MM-dd");
            if (RouteId != null)
            {
                logger.LogDebug("Getting user route {RouteId}", RouteId);
                var userRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (userRoute == null || userRoute.RouteType != Models.Route.PrivateRoute || userRoute.Creator != _userAccount.Id)
                    return;

                DistanceType =
                    userRoute.RouteType == Models.Route.SystemRoute ? 1 :
                    string.IsNullOrEmpty(userRoute.MapPoints) ? 2 :
                    3;
                DistanceManual = DistanceType == 2 ? decimal.Round(userService.ToUserDistanceUnits(userRoute.Distance, _userAccount), 4) : null;
                MapName = userRoute.Name;
                MapNotes = userRoute.Notes;
                MapPoints = userRoute.MapPoints;
                MapDistance = userRoute.Distance;
            }
            return;
        }

        var existingActivity = await runLogRepository.GetRunLogAsync(ActivityId.Value);
        if (existingActivity == null || existingActivity.UserAccountId != _userAccount.Id)
        {
            logger.LogWarning("RunLog {ActivityId} not found or doesn't belong to user {UserAccountId}", ActivityId, _userAccount.Id);
            ActivityId = null;
            return;
        }

        Date = existingActivity.Date.ToString("yyyy-MM-dd");
        TimeTaken = paceService.TimeTakenDisplayFormat(existingActivity.TimeTaken);
        Comment = existingActivity.Comment;
        DistanceType =
            existingActivity.Route.RouteType == Models.Route.SystemRoute ? 1 :
            string.IsNullOrEmpty(existingActivity.Route.MapPoints) ? 2 :
            3;
        DistanceManual = DistanceType == 2 ? decimal.Round(userService.ToUserDistanceUnits(existingActivity.Route.Distance, _userAccount), 4) : null;
        RouteId = existingActivity.Route.Id;
        MapName = existingActivity.Route.Name;
        MapNotes = existingActivity.Route.Notes;
        MapPoints = existingActivity.Route.MapPoints;
        MapDistance = existingActivity.Route.Distance;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!string.IsNullOrEmpty(Cancel))
            return Redirect(GetRedirectUri());

        _userAccount = await userAccountRepository.GetUserAccountAsync(User);

        if (!string.IsNullOrEmpty(Add))
        {
            if (await AddRunAsync(_userAccount) == null)
                return BadRequest();
        }
        else if (!string.IsNullOrEmpty(Save) && ActivityId != null)
        {
            if (!await SaveRunAsync(_userAccount, ActivityId.Value))
                return BadRequest();
        }
        else if (!string.IsNullOrEmpty(Delete) && ActivityId != null)
        {
            await DeleteRunAsync(_userAccount, ActivityId.Value);
        }
        return Redirect(GetRedirectUri());

        string GetRedirectUri() => $"/runlog?date={Date.ParseDateTime():yyyy-MM-dd}";
    }

    public async Task<string> UserUnitsAsync()
        => (DistanceUnits)(_userAccount ??= await userAccountRepository.GetUserAccountAsync(User)).DistanceUnits
            switch { DistanceUnits.Miles => "miles", DistanceUnits.Kilometers => "km", _ => "" };

    public async Task<decimal> UserUnitsMultiplierAsync()
        => (DistanceUnits)(_userAccount ??= await userAccountRepository.GetUserAccountAsync(User)).DistanceUnits
            switch { DistanceUnits.Miles => 1000 * UserService.KilometersToMiles, DistanceUnits.Kilometers => 1000, _ => 1 };

    private async Task<Models.RunLog?> AddRunAsync(UserAccount userAccount, Models.RunLog? replacedRunLog = null)
    {
        logger.LogInformation("Adding new run");
        Models.RunLog? newRunLog;
        switch (DistanceType)
        {
            case 1:
                if (RouteId == null || RouteId == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return null;

                logger.LogDebug("Getting system route {RouteId}", RouteId);
                var systemRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (systemRoute == null || systemRoute.RouteType != Models.Route.SystemRoute)
                    return null;

                logger.LogDebug("Creating a new run log entry");
                newRunLog = await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), systemRoute, TimeTaken!, Comment, replacedRunLog);

                break;
            case 2:
                if (DistanceManual == null || DistanceManual == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return null;

                var distanceInMeters = userService.ToDistanceInMeters(DistanceManual ?? 0, userAccount);
                logger.LogDebug("Creating a new manual distance route for manual distance: {DistanceManual}", DistanceManual);
                var manualRoute = await routeRepository.CreateNewRouteAsync(
                    userAccount,
                    userService.ToUserDistance(distanceInMeters, userAccount),
                    "",
                    distanceInMeters,
                    "",
                    null);

                logger.LogDebug("Creating a new run log entry");
                newRunLog = await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), manualRoute, TimeTaken!, Comment, replacedRunLog);

                break;
            case 3:
                if (RouteId == null || RouteId == 0 || Date == null || paceService.TimeTaken(TimeTaken) == null)
                    return null;

                logger.LogDebug("Getting user route {RouteId}", RouteId);
                var userRoute = await routeRepository.GetRouteAsync(RouteId.Value);
                if (userRoute == null || userRoute.RouteType != Models.Route.PrivateRoute || userRoute.Creator != userAccount.Id)
                    return null;

                logger.LogDebug("Creating a new run log entry");
                newRunLog = await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), userRoute, TimeTaken!, Comment, replacedRunLog);

                break;
            case 4:
                if (MapDistance == null || MapDistance == 0 ||
                    string.IsNullOrEmpty(MapName) ||
                    string.IsNullOrEmpty(MapPoints) ||
                    Date == null ||
                    paceService.TimeTaken(TimeTaken) == null)
                {
                    return null;
                }

                logger.LogDebug("Creating a new mapped route for {MapName} ({MapNotes}): {MapPoints}", MapName, MapNotes, MapPoints);
                var newUserRoute = await routeRepository.CreateNewRouteAsync(userAccount, MapName, MapPoints, MapDistance.Value, MapNotes, replacedRunLog?.RouteId);

                logger.LogDebug("Creating a new run log entry");
                newRunLog = await runLogRepository.CreateNewAsync(userAccount, Date.ParseDateTime(), newUserRoute, TimeTaken!, Comment, replacedRunLog);

                break;
            default:
                return null;
        }

        return newRunLog;
    }

    private async Task<bool> SaveRunAsync(UserAccount userAccount, int activityId)
    {
        logger.LogInformation("Saving run {Id}", activityId);
        var currentActivity = await DeleteRunAsync(userAccount, activityId, true);
        if (currentActivity == null)
            return false;

        var newActivity = await AddRunAsync(userAccount, currentActivity);
        if (newActivity == null)
            return false;

        return true;
    }

    private async Task<Models.RunLog?> DeleteRunAsync(UserAccount userAccount, int activityId, bool deleteRoute = false)
    {
        logger.LogInformation("Deleting run {Id}", activityId);
        var existingActivity = await runLogRepository.GetRunLogAsync(activityId);
        if (existingActivity == null || existingActivity.UserAccountId != userAccount.Id)
        {
            logger.LogWarning("RunLog {ActivityId} not found or doesn't belong to user {UserAccountId}", activityId, userAccount.Id);
            return null;
        }

        if (DistanceType == 4 && deleteRoute && existingActivity.Route.RouteType == Models.Route.PrivateRoute)
        {
            logger.LogInformation("Deleting route {RouteId}", existingActivity.RouteId);
            await routeRepository.DeleteRouteAsync(existingActivity.Route);
        }

        await runLogRepository.DeleteRunLogAsync(existingActivity);
        return existingActivity;
    }
}
