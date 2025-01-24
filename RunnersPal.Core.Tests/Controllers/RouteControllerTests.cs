using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class RouteControllerTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Given_two_mapped_routes_and_one_manual_distance_route_Should_return_only_the_two_mapped_routes()
    {
        await CreateRouteAsync("route 1", 3000, true);
        await CreateRouteAsync("manual distance 1", 5000, false);
        await CreateRouteAsync("route 2", 4000, true);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/api/route/list");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var routes = await response.Content.ReadFromJsonAsync<RouteListApiModel>();
        Assert.IsNotNull(routes);
        Assert.AreEqual(2, routes.Routes.Count());
        Assert.IsTrue(routes.Routes.Any(x => x.Name == "route 1"));
        Assert.IsTrue(routes.Routes.Any(x => x.Name == "route 2"));
        Assert.AreEqual(1, routes.Pagination.PageCount);
        Assert.AreEqual(1, routes.Pagination.PageNumber);
        Assert.AreEqual(1, routes.Pagination.Pages.Count());
        Assert.IsTrue(routes.Pagination.Pages.Single().IsSelected);
    }

    [TestMethod]
    public async Task Given_no_mapped_routes_Should_return_empty_routes()
    {
        await CreateRouteAsync("manual distance 1", 5000, false);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/api/route/list");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var routes = await response.Content.ReadFromJsonAsync<RouteListApiModel>();
        Assert.IsNotNull(routes);
        Assert.AreEqual(0, routes.Routes.Count());
        Assert.AreEqual(1, routes.Pagination.PageCount);
        Assert.AreEqual(1, routes.Pagination.PageNumber);
        Assert.AreEqual(1, routes.Pagination.Pages.Count());
        Assert.IsTrue(routes.Pagination.Pages.Single().IsSelected);
    }

    [TestMethod]
    public async Task Given_a_search_criteria_Should_return_matching_routes()
    {
        await CreateRouteAsync("route 1", 3000, true);
        await CreateRouteAsync("manual distance 1 match", 5000, false, "this should not match as it's a manual distance");
        await CreateRouteAsync("route 2", 4000, true, "this should match too");
        await CreateRouteAsync("route 3", 4000, true);
        await CreateRouteAsync("route 4 match", 4000, true);
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/api/route/list?find=match");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var routes = await response.Content.ReadFromJsonAsync<RouteListApiModel>();
        Assert.IsNotNull(routes);
        Assert.AreEqual(2, routes.Routes.Count());
        Assert.IsTrue(routes.Routes.Any(x => x.Name == "route 2"));
        Assert.IsTrue(routes.Routes.Any(x => x.Name == "route 4 match"));
        Assert.AreEqual(1, routes.Pagination.PageCount);
        Assert.AreEqual(1, routes.Pagination.PageNumber);
        Assert.AreEqual(1, routes.Pagination.Pages.Count());
        Assert.IsTrue(routes.Pagination.Pages.Single().IsSelected);
    }

    [TestMethod]
    [DataRow(30, 1, 30, 1, 1)]
    [DataRow(30, 2, 30, 1, 1)]
    [DataRow(31, 1, 30, 2, 1)]
    [DataRow(31, 2, 1, 2, 2)]
    [DataRow(60, 1, 30, 2, 1)]
    [DataRow(60, 2, 30, 2, 2)]
    [DataRow(60, 3, 30, 2, 2)]
    [DataRow(61, 1, 30, 3, 1)]
    [DataRow(61, 2, 30, 3, 2)]
    [DataRow(61, 3, 1, 3, 3)]
    public async Task Given_a_page_number_Should_return_routes(int totalRoutes, int pageNumber,
        int expectedNumberOfRoutesReturned, int expectedPageCount, int expectedPageNumber)
    {
        foreach (var routeNum in Enumerable.Range(1, totalRoutes))
            await CreateRouteAsync($"route {routeNum}", 3000 + routeNum, true);

        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/api/route/list?pagenumber={pageNumber}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var routes = await response.Content.ReadFromJsonAsync<RouteListApiModel>();
        Assert.IsNotNull(routes);
        Assert.AreEqual(expectedNumberOfRoutesReturned, routes.Routes.Count());
        Assert.AreEqual(expectedPageCount, routes.Pagination.PageCount);
        Assert.AreEqual(expectedPageNumber, routes.Pagination.PageNumber);
        Assert.AreEqual(expectedPageCount, routes.Pagination.Pages.Count());
    }

    private async Task<Route> CreateRouteAsync(string name, decimal distance, bool isMappedRoute, string? notes = null)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Name = name,
            RouteType = Route.PrivateRoute,
            MapPoints = isMappedRoute ? "[]" : "", // just needs to be a non-empty string to be considered a saved, mapped route
            Distance = distance,
            DistanceUnits = (int)DistanceUnits.Meters,
            Notes = notes
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }
}