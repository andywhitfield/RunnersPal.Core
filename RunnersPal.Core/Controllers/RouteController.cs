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
    IRouteRepository routeRepository,
    IUserService userService)
    : ControllerBase
{
    [HttpGet("list")]
    public async Task<RouteListApiModel> List([FromQuery] int pageNumber)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting routes for user account id = {UserAccountId}", userAccount.Id);
        var routes = Pagination.Paginate(await routeRepository.GetRoutesByUserAsync(userAccount), pageNumber);
        return new(new Pagination(routes.Page, routes.PageCount), routes.Items.Select(r => new RouteApiModel(r.Id, r.Name, userService.ToUserDistance(r.Distance, userAccount))));
    }
}
