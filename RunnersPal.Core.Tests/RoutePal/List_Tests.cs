using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RoutePal;

[TestClass]
public class List_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task No_saved_routes()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/routepal/list");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("You have no saved routes", responseContent);
    }

    [TestMethod]
    public async Task List_all_saved_routes()
    {
        await CreateRouteAsync("Test Route 1", "[]", 1000);
        await CreateRouteAsync("Test Route 2", "[]", 2000);
        await CreateRouteAsync("Test Route 3", "[]", 1300);
        await LogRunAsync("Test Route 1", new(2026, 2, 13));
        await LogRunAsync("Test Route 3", new(2026, 2, 14));
        await LogRunAsync("Test Route 2", new(2026, 2, 15));
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/routepal/list");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var indexOfRoute1 = responseContent.IndexOf("Test Route 1", StringComparison.Ordinal);
        var indexOfRoute2 = responseContent.IndexOf("Test Route 2", StringComparison.Ordinal);
        var indexOfRoute3 = responseContent.IndexOf("Test Route 3", StringComparison.Ordinal);
        Assert.IsGreaterThan(0, indexOfRoute1);
        Assert.IsGreaterThan(0, indexOfRoute2);
        Assert.IsGreaterThan(0, indexOfRoute3);
        // should be in order of most recently run by default, so route 2 should be first, then route 3, then route 1
        Assert.IsTrue(indexOfRoute2 < indexOfRoute3 && indexOfRoute3 < indexOfRoute1);
    }

    [TestMethod]
    public async Task List_all_saved_routes_by_distance()
    {
        await CreateRouteAsync("Test Route 1", "[]", 1000);
        await CreateRouteAsync("Test Route 2", "[]", 2000);
        await CreateRouteAsync("Test Route 3", "[]", 1300);
        await LogRunAsync("Test Route 1", new(2026, 2, 13));
        await LogRunAsync("Test Route 3", new(2026, 2, 14));
        await LogRunAsync("Test Route 2", new(2026, 2, 15));
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/routepal/list?sort=distance");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var indexOfRoute1 = responseContent.IndexOf("Test Route 1", StringComparison.Ordinal);
        var indexOfRoute2 = responseContent.IndexOf("Test Route 2", StringComparison.Ordinal);
        var indexOfRoute3 = responseContent.IndexOf("Test Route 3", StringComparison.Ordinal);
        Assert.IsGreaterThan(0, indexOfRoute1);
        Assert.IsGreaterThan(0, indexOfRoute2);
        Assert.IsGreaterThan(0, indexOfRoute3);
        // now in distance order, so route 1 should be first, then route 3, then route 2
        Assert.IsTrue(indexOfRoute1 < indexOfRoute3 && indexOfRoute3 < indexOfRoute2);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();

    private async Task CreateRouteAsync(string routeName, string mapPoints, decimal distance)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            Name = routeName,
            MapPoints = mapPoints,
            CreatorAccount = context.UserAccount.Single(u => u.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Distance = distance,
            DistanceUnits = (int)DistanceUnits.Meters,
            RouteType = Route.PrivateRoute
        });
        await context.SaveChangesAsync();
    }

    private async Task LogRunAsync(string routeName, DateTime dateOfRun)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var userAccount = context.UserAccount.Single(u => u.EmailAddress == TestStubAuthHandler.TestUserEmail);
        var route = await context.Route.FirstAsync(r => r.Name == routeName);
        var newRun = context.RunLog.Add(new()
        {
            Route = route,
            UserAccount = userAccount,
            Date = dateOfRun,
            TimeTaken = "00:10:00"
        });
        await context.SaveChangesAsync();
    }
}
