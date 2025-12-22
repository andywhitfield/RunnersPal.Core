using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class RouteController_ShareTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Can_share_route()
    {
        var route = await CreateRouteAsync("route 1", 3000, true);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.PostAsync("/api/route/share/" + route.Id, null);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var shared = await response.Content.ReadFromJsonAsync<ShareApiModel>();
        Assert.IsNotNull(shared);
        var shareUrlPrefix = "http://localhost/RoutePal/Map?shareLink=";
        Assert.StartsWith(shareUrlPrefix, shared.ShareLink);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = await context.Route.SingleAsync(r => r.Id == route.Id);
        Assert.IsFalse(string.IsNullOrEmpty(updatedRoute.ShareLink));
        Assert.AreEqual(shared.ShareLink[shareUrlPrefix.Length..], updatedRoute.ShareLink);
    }

    [TestMethod]
    public async Task Cannot_share_deleted_route()
    {
        var route = await CreateRouteAsync("route 1", 3000, true, routeType: Route.DeletedRoute);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.PostAsync("/api/route/share/" + route.Id, null);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Cannot_share_manual_distance_route()
    {
        var route = await CreateRouteAsync("route 1", 3000, false, routeType: Route.DeletedRoute);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.PostAsync("/api/route/share/" + route.Id, null);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task Cannot_share_other_users_route()
    {
        var route = await CreateOtherUserRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.PostAsync("/api/route/share/" + route.Id, null);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = await context.Route.SingleAsync(r => r.Id == route.Id);
        Assert.IsNull(updatedRoute.ShareLink);
    }

    [TestMethod]
    public async Task Can_unshare_route()
    {
        var route = await CreateRouteAsync("route 1", 3000, true, "test-share");
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.DeleteAsync("/api/route/share/" + route.Id);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = await context.Route.SingleAsync(r => r.Id == route.Id);
        Assert.IsNull(updatedRoute.ShareLink);
    }

    [TestMethod]
    public async Task Should_do_nothing_if_unsharing_an_unshared_route()
    {
        var route = await CreateRouteAsync("route 1", 3000, true);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.DeleteAsync("/api/route/share/" + route.Id);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        // sharelink should still be null
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = await context.Route.SingleAsync(r => r.Id == route.Id);
        Assert.IsNull(updatedRoute.ShareLink);
    }

    [TestMethod]
    public async Task Cannot_unshare_other_users_route()
    {
        var route = await CreateOtherUserRouteAsync("test-share-link");
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.DeleteAsync("/api/route/share/" + route.Id);
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);

        // sharelink should still be set
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = await context.Route.SingleAsync(r => r.Id == route.Id);
        Assert.AreEqual("test-share-link", updatedRoute.ShareLink);
    }

    private async Task<Route> CreateRouteAsync(string name, decimal distance, bool isMappedRoute, string? shareLink = null, char routeType = Route.PrivateRoute)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Name = name,
            RouteType = routeType,
            MapPoints = isMappedRoute ? "[]" : "", // just needs to be a non-empty string to be considered a saved, mapped route
            Distance = distance,
            DistanceUnits = (int)DistanceUnits.Meters,
            ShareLink = shareLink
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    private async Task<Route> CreateOtherUserRouteAsync(string? shareLink = null)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var otherUser = context.UserAccount.Add(new() { EmailAddress = "other-user-email", DisplayName = "", OriginalHostAddress = "" });
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = otherUser.Entity,
            Name = "other-route",
            RouteType = Route.PrivateRoute,
            MapPoints = "[]",
            Distance = 1000,
            DistanceUnits = (int)DistanceUnits.Meters,
            ShareLink = shareLink
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }
}