using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.RoutePal;

[Authorize]
public class ListModel(IUserAccountRepository userAccountRepository,
    IUserService userService,
    IUserRouteService userRouteService)
    : PageModel
{
    private Dictionary<int, Models.RunLog> _lastRunsForRoutes = [];
    private UserAccount? _userAccount;
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public string? Find { get; set; }
    public IEnumerable<Models.Route> Routes { get; private set; } = [];
    public Pagination Pagination { get; private set; } = Pagination.Empty;

    public async Task OnGet()
    {
        _userAccount = await userAccountRepository.GetUserAccountAsync(User);
        Find = string.IsNullOrEmpty(Find) ? null : Find.Trim();
        var (userRoutes, lastRunsForRoutes) = await userRouteService.GetUserRoutesAsync(_userAccount, Find);
        _lastRunsForRoutes = lastRunsForRoutes;
        var routes = Pagination.Paginate(userRoutes, PageNumber);
        Routes = routes.Items;
        Pagination = new(routes.Page, routes.PageCount);
    }

    public async Task<string> RouteDistanceAsync(Models.Route route)
        => userService.ToUserDistance(route.Distance, _userAccount ??= await userAccountRepository.GetUserAccountAsync(User));

    public DateOnly? LastRun(Models.Route route)
        => _lastRunsForRoutes.TryGetValue(route.Id, out var runLog) ? DateOnly.FromDateTime(runLog.Date) : null;
}
