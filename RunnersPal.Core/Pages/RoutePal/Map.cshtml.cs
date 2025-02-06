using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RoutePal;

public class MapModel(ILogger<MapModel> logger,
    IUserService userService,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IConfiguration configuration)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }
    [BindProperty(SupportsGet = true)] public bool? LoadUnsaved { get; set; }
    [BindProperty(SupportsGet = true)] public string? Unit { get; set; }
    [BindProperty] public string? RouteName { get; set; }
    [BindProperty] public string? RouteNotes { get; set; }
    [BindProperty] public string? Points { get; set; }
    [BindProperty] public decimal Distance { get; set; }
    [BindProperty] public string? Delete { get; set; }
    public bool IsRouteDeleted { get; private set; }
    public bool IsLoggedIn => userService.IsLoggedIn;
    public bool IsElevationFeatureEnabled => configuration.GetValue("ElevationFeatureEnabled", false);

    public async Task<IActionResult> OnGet()
    {
        if (RouteId != null)
        {
            if (!userService.IsLoggedIn)
            {
                logger.LogInformation("No User authenticated, cannot load any route");
                return BadRequest();
            }

            var userAccount = await userAccountRepository.GetUserAccountAsync(User);
            var route = await routeRepository.GetRouteAsync(RouteId.Value);
            if (route == null || route.Creator != userAccount.Id)
            {
                logger.LogWarning("Route {RouteId} is not owned by user {UserAccountId}, cannot view this route", RouteId, userAccount.Id);
                return BadRequest();
            }

            RouteName = route.Name;
            Points = route.MapPoints ?? "";
            Distance = route.Distance;
            RouteNotes = route.Notes ?? "";
            IsRouteDeleted = route.RouteType == Models.Route.DeletedRoute;
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!userService.IsLoggedIn)
        {
            logger.LogWarning("No User authenticated, redirecting to login page");
            return Redirect($"/signin?ReturnUrl={WebUtility.UrlEncode("/routepal/map?loadunsaved=true")}");
        }
        if (string.IsNullOrWhiteSpace(RouteName) || string.IsNullOrWhiteSpace(Points))
        {
            logger.LogInformation("Route is missing a name or points, cannot save");
            return BadRequest();
        }

        var userAccount = await userAccountRepository.GetUserAccountAsync(User);

        if (RouteId != null)
        {
            var route = await routeRepository.GetRouteAsync(RouteId.Value);
            if (route?.Creator != userAccount.Id)
            {
                logger.LogWarning("Route {RouteId} not found or is not owned by user {UserAccountId}, cannot update this route", RouteId, userAccount.Id);
                return BadRequest();
            }

            if (route.RouteType != Models.Route.PrivateRoute)
            {
                logger.LogWarning("Route {RouteId} is not an active, private route, cannot save", RouteId);
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(Delete))
            {
                await routeRepository.DeleteRouteAsync(route);
                return Redirect("/routepal");
            }

            var updatedRoute = await routeRepository.UpdateRouteAsync(route, userAccount, RouteName, Points, Distance, RouteNotes);
            RouteId = updatedRoute.Id;
        }
        else if (string.IsNullOrEmpty(Delete))
        {
            var newRoute = await routeRepository.CreateNewRouteAsync(userAccount, RouteName, Points, Distance, RouteNotes, null);
            RouteId = newRoute.Id;
        }
        else
        {
            logger.LogInformation("No route id, but delete has been set - cannot delete an unknown route");
            return BadRequest();
        }

        return Redirect($"/routepal/map?routeid={RouteId}");
    }

    public async Task<string> UserUnitsAsync()
        => !userService.IsLoggedIn && !string.IsNullOrEmpty(Unit)
            ? string.Equals(Unit, "miles", StringComparison.InvariantCultureIgnoreCase) ? "miles" : "km"
            : (Models.DistanceUnits?)(await userAccountRepository.GetUserAccountOrNullAsync(User))?.DistanceUnits switch { Models.DistanceUnits.Miles => "miles", Models.DistanceUnits.Kilometers => "km", _ => "km" };

    public async Task<decimal> UserUnitsMultiplierAsync()
        => !userService.IsLoggedIn && !string.IsNullOrEmpty(Unit)
            ? string.Equals(Unit, "miles", StringComparison.InvariantCultureIgnoreCase) ? 1000 * UserService.KilometersToMiles : 1000
            : (Models.DistanceUnits?)(await userAccountRepository.GetUserAccountOrNullAsync(User))?.DistanceUnits switch { Models.DistanceUnits.Miles => 1000 * UserService.KilometersToMiles, Models.DistanceUnits.Kilometers => 1000, _ => 1000 };

    public string SwitchToUnit => string.Equals(Unit, "miles", StringComparison.InvariantCultureIgnoreCase) ? "km" : "miles";
}
