using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Models;
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
    public async Task<RouteListApiModel> List([FromQuery] int pageNumber, [FromQuery] string? find, [FromQuery] string? sort)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting routes for user account id = {UserAccountId} with find filter = {Find} with sort = {Sort}", userAccount.Id, find, sort);
        find = string.IsNullOrEmpty(find) ? null : find.Trim();
        var (userRoutes, lastRunsForRoutes) = await userRouteService.GetUserRoutesAsync(userAccount, find, sort);
        var routes = Pagination.Paginate(userRoutes, pageNumber);
        return new(
            new Pagination(routes.Page, routes.PageCount),
            routes.Items.Select(r => new RouteApiModel(r.Id, r.Name, userService.ToUserDistance(r.Distance, userAccount), lastRunsForRoutes.TryGetValue(r.Id, out var runLog) ? DateOnly.FromDateTime(runLog.Date).ToString("D") : "")));
    }

    [AllowAnonymous, HttpPost("share/new")] // as this is anonymous, it might be worth thinking about rate limiting / restrict the number of points / etc.
    public async Task<ShareApiModel> ShareNew([FromForm] string points, [FromForm] string name, [FromForm] decimal distance, [FromForm] string? notes = null)
    {
        logger.LogDebug("Sharing a new un-saved route with [{Points}] points, name = {RouteName}, notes = {Notes}", points, name, notes);
        UserAccount userAccount;
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            logger.LogDebug("User is authenticated, sharing an un-saved route");
            userAccount = await userAccountRepository.GetUserAccountAsync(User);
        }
        else
        {
            logger.LogDebug("User is not authenticated, sharing an un-saved route for an anonymous user");
            userAccount = await userAccountRepository.GetAdminUserAccountAsync();
        }
        
        var shareLink = await routeRepository.CreateUnsavedRouteShareLinkAsync(userAccount, name, points, distance, notes);
        var shareUri = Url.PageLink(pageName: "/routepal/map", values: new { shareLink }, protocol: Request.Scheme) ?? throw new InvalidOperationException("Could not generate share link");
        return new(shareUri);
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
