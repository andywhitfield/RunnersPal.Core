using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages.RoutePal;

public class MapModel(IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }
    [BindProperty] public string? RouteName { get; set; }
    [BindProperty] public string? Points { get; set; }

    public void OnGet() { }

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
