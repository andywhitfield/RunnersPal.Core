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
    IUserRouteService userRouteService,
    IRouteRepository routeRepository)
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

    [HttpPost("share/{routeId}")]
    public async Task<ShareApiModel> Share([FromRoute] int routeId)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        var route = await routeRepository.GetRouteAsync(routeId);
        if (route?.Creator != userAccount.Id)
        {
            logger.LogWarning("Route {RouteId} is not owned by user {UserAccountId}, cannot create a share link for this route", routeId, userAccount.Id);
            throw new InvalidOperationException("Cannot share a route you do not own");
        }
        if (route.RouteType == Models.Route.DeletedRoute || string.IsNullOrEmpty(route.MapPoints) || !string.IsNullOrEmpty(route.ShareLink))
        {
            logger.LogWarning("Route {RouteId} already has a share link, is deleted, or has no map points, cannot create a share link for this route", routeId);
            throw new InvalidOperationException("Cannot share a route which already has a share link, is deleted, or is an empty route");
        }

        var shareLink = await routeRepository.GenerateShareLinkAsync(route);
        var shareUri = Url.PageLink(pageName: "/routepal/map", values: new { shareLink }, protocol: Request.Scheme) ?? throw new InvalidOperationException("Could not generate share link");
        return new(shareUri);
    }

    [HttpDelete("share/{routeId}")]
    public async Task Unshare([FromRoute] int routeId)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        var route = await routeRepository.GetRouteAsync(routeId);
        if (route?.Creator != userAccount.Id)
        {
            logger.LogWarning("Route {RouteId} is not owned by user {UserAccountId}, cannot remove the share link for this route", routeId, userAccount.Id);
            throw new InvalidOperationException("Cannot share a route you do not own");
        }
        if (string.IsNullOrEmpty(route.ShareLink))
        {
            logger.LogWarning("Route {RouteId} has no share link, nothing to do", routeId);
            return;
        }

        await routeRepository.RemoveShareLinkAsync(route);
    }
}
