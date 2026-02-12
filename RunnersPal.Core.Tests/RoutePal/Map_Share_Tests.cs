using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RoutePal;

[TestClass]
public class Map_Share_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task When_using_share_link_for_own_route_Should_redirect()
    {
        var route = await CreateTestRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=" + route.ShareLink);
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/routepal/map?routeid=" + route.Id, response.Headers.Location?.ToString());
    }

    [TestMethod]
    public async Task When_using_share_link_for_another_route_Should_show_route()
    {
        await CreateTestRouteAsync();
        var route = await CreateTestRouteForOtherUserAsync();
        using var client = _webApplicationFactory.CreateClient(true, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=" + route.ShareLink);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains(route.Name, page);
        Assert.DoesNotContain("type=\"submit\" name=\"save\"", page, message: "Save button should not be present");
    }

    [TestMethod]
    public async Task Give_unauthenticated_user_When_using_share_link_for_another_route_Should_show_route()
    {
        await CreateTestRouteAsync();
        var route = await CreateTestRouteForOtherUserAsync();
        using var client = _webApplicationFactory.CreateClient(false, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=" + route.ShareLink);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains(route.Name, page);
        Assert.DoesNotContain("type=\"submit\" name=\"save\"", page, message: "Save button should not be present");
    }

    [TestMethod]
    public async Task Given_a_share_link_and_routeid_Should_return_bad_request()
    {
        var route = await CreateTestRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=" + route.ShareLink + "&routeid=" + route.Id);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Given_a_share_link_to_a_route_without_map_points_Should_return_bad_request()
    {
        await CreateTestRouteAsync();
        var route = await CreateManualDistanceRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=" + route.ShareLink);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        async Task<Route> CreateManualDistanceRouteAsync()
        {
            await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
            var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var otherUserAccount = context.UserAccount.Add(new() { DisplayName = "other user", OriginalHostAddress = "" });
            var otherUsersRoute = context.Route.Add(
                new() { CreatorAccount = otherUserAccount.Entity, Name = "other user route 3", Distance = 3000, RouteType = Route.PrivateRoute, ShareLink = "testroute3" }
            );
            await context.SaveChangesAsync();
            return otherUsersRoute.Entity;
        }
    }

    [TestMethod]
    public async Task Given_a_share_link_which_does_not_exist_should_redirect_to_map_notfound_page()
    {
        var route = await CreateTestRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, allowAutoRedirect: false);
        using var response = await client.GetAsync("/routepal/map?sharelink=doesnotexist");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/routepal/mapnotfound?sharelink=doesnotexist", response.Headers.Location?.ToString());
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    private async Task<Route> CreateTestRouteAsync()
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            Name = "test-route",
            MapPoints = "[{'lat':50,'lng':1}]",
            CreatorAccount = context.UserAccount.Single(u => u.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Distance = 1600,
            DistanceUnits = (int)DistanceUnits.Meters,
            RouteType = Route.PrivateRoute,
            ShareLink = "testroute1"
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    private async Task<Route> CreateTestRouteForOtherUserAsync()
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var otherUserAccount = context.UserAccount.Add(new() { DisplayName = "other user", OriginalHostAddress = "" });
        var otherUsersRoute = context.Route.Add(
            new() { CreatorAccount = otherUserAccount.Entity, Name = "other user route 2", Distance = 2500, RouteType = Route.PrivateRoute, MapPoints = "[]", ShareLink = "testroute2" }
        );
        await context.SaveChangesAsync();
        return otherUsersRoute.Entity;
    }
}
