using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages.RoutePal;

public class MapModel(ILogger<MapModel> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }
    [BindProperty] public string? RouteName { get; set; }
    [BindProperty] public string? RouteNotes { get; set; }
    [BindProperty] public string? Points { get; set; }
    [BindProperty] public decimal Distance { get; set; }
    [BindProperty] public string? Delete { get; set; }

    public async Task<IActionResult> OnGet([FromQuery] int? routeId)
    {
        if (routeId != null)
        {
            if (User == null)
            {
                logger.LogInformation("No User authenticated, cannot load any route");
                return BadRequest();
            }

            var userAccount = await userAccountRepository.GetUserAccountAsync(User);
            var route = await routeRepository.GetRouteAsync(routeId.Value);
            if (route == null || route.CreatorAccount.Id != userAccount.Id)
            {
                logger.LogWarning("Route {RouteId} is not owned by user {UserAccountId}, cannot view this route", routeId, userAccount.Id);
                return BadRequest();
            }

            RouteName = route.Name;
            Points = route.MapPoints ?? "";
            Distance = route.Distance;
            RouteNotes = route.Notes ?? "";
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (User == null)
        {
            logger.LogWarning("No User authenticated, cannot save any route");
            return BadRequest();
        }
        if (string.IsNullOrEmpty(RouteName) || string.IsNullOrEmpty(Points))
        {
            logger.LogInformation("Route is missing a name of points, cannot save");
            return BadRequest();
        }

        var userAccount = await userAccountRepository.GetUserAccountAsync(User);

        if (RouteId != null)
        {
            var route = await routeRepository.GetRouteAsync(RouteId.Value);
            if (route?.Creator != userAccount.Id)
            {
                logger.LogWarning("Route {RouteId} is not owned by user {UserAccountId}, cannot update this route", RouteId, userAccount.Id);
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
            var newRoute = await routeRepository.CreateNewRouteAsync(userAccount, RouteName, Points, Distance, RouteNotes);
            RouteId = newRoute.Id;
        }
        else
        {
            logger.LogInformation("No route id, but delete has been set - cannot delete an unknown route");
            return BadRequest();
        }

        return Redirect($"/routepal/map?routeid={RouteId}");
    }
}
