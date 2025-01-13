using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class RouteController(ILogger<RouteController> logger,
    IUserAccountRepository userAccountRepository,
    IUserService userService,
    IUserRouteService userRouteService)
    : ControllerBase
{
    [HttpGet("list")]
    public async Task<RouteListApiModel> List([FromQuery] int pageNumber, [FromQuery] string? find)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting routes for user account id = {UserAccountId} with find filter = {Find}", userAccount.Id, find);
        find = string.IsNullOrEmpty(find) ? null : find.Trim();
        var (userRoutes, lastRunsForRoutes) = await userRouteService.GetUserRoutesAsync(userAccount, find);
        var routes = Pagination.Paginate(userRoutes, pageNumber);
        return new(
            new Pagination(routes.Page, routes.PageCount),
            routes.Items.Select(r => new RouteApiModel(r.Id, r.Name, userService.ToUserDistance(r.Distance, userAccount), lastRunsForRoutes.TryGetValue(r.Id, out var runLog) ? DateOnly.FromDateTime(runLog.Date).ToString("D") : "")));
    }
}
