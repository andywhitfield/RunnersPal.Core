using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RoutePal;

[Authorize]
public class ListModel(IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public IEnumerable<Models.Route> Routes { get; private set; } = [];
    public Pagination Pagination { get; private set; } = Pagination.Empty;

    public async Task OnGet()
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        var routes = Pagination.Paginate(await routeRepository.GetRoutesByUserAsync(userAccount), PageNumber);
        Routes = routes.Items;
        Pagination = new(routes.Page, routes.PageCount);
    }
}
