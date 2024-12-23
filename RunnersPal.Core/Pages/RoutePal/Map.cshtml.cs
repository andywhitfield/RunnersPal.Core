using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages.RoutePal;

public class MapModel(IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }
    [BindProperty] public string? RouteName { get; set; }
    [BindProperty] public string? Points { get; set; }

    public async Task<IActionResult> OnGet([FromQuery] int? routeId)
    {
        if (routeId != null)
        {
            if (User == null)
                return BadRequest();

            var userAccount = await userAccountRepository.GetUserAccountAsync(User);
            var route = await routeRepository.GetRouteAsync(routeId.Value);
            if (route == null || route.CreatorAccount != userAccount)
                return BadRequest();
            
            RouteName = route.Name;
            Points = route.MapPoints ?? "";
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (User == null)
            return Unauthorized();
        if (string.IsNullOrEmpty(RouteName) || string.IsNullOrEmpty(Points))
            return BadRequest();

        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        var newRoute = await routeRepository.CreateNewRouteAsync(userAccount, RouteName, Points, null);

        return Redirect($"/routepal/map?routeid={newRoute.Id}");
    }
}
