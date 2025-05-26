using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogActivity_Add_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Should_add_new_activity_using_manual_distance()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "add", "Add" },
            { "date", "2024-01-23" },
            { "timetaken", "1:02:45" },
            { "distancetype", "2" },
            { "distancemanual", "5" }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-23", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var runActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 23, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(runActivity);
        Assert.IsNull(runActivity.Comment);
        Assert.AreEqual(Models.RunLog.LogStateValid, runActivity.LogState);
        Assert.AreEqual("1:02:45", runActivity.TimeTaken);
        var runActivityRoute = await context.Route.FindAsync(runActivity.RouteId);
        Assert.IsNotNull(runActivityRoute);
        Assert.AreEqual("", runActivityRoute.MapPoints);
        Assert.AreEqual("5miles", runActivityRoute.Name);
        Assert.AreEqual(8046.72m, runActivityRoute.Distance);
        Assert.AreEqual((int)DistanceUnits.Meters, runActivityRoute.DistanceUnits);
    }

    [TestMethod]
    public async Task Should_add_new_activity_using_system_route()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        var systemRoute = await GetSystemRouteAsync("5 Kilometers");
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "add", "Add" },
            { "date", "2024-01-22" },
            { "timetaken", "29:21" },
            { "distancetype", "1" },
            { "routeid", systemRoute.Id.ToString() }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-22", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var runActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 22, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(runActivity);
        Assert.IsNull(runActivity.Comment);
        Assert.AreEqual(Models.RunLog.LogStateValid, runActivity.LogState);
        Assert.AreEqual("29:21", runActivity.TimeTaken);
        Assert.AreEqual(systemRoute.Id, runActivity.RouteId);

        async Task<Models.Route> GetSystemRouteAsync(string routeName)
        {
            await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
            await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            return await context.Route.SingleAsync(x => x.Name == routeName && x.RouteType == Models.Route.SystemRoute);
        }
    }

    [TestMethod]
    public async Task Should_add_new_activity_using_saved_route()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        var userRoute = await CreateRouteAsync();
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "add", "Add" },
            { "date", "2024-01-21" },
            { "timetaken", "29:21" },
            { "distancetype", "3" },
            { "routeid", userRoute.Id.ToString() }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-21", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var runActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 21, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(runActivity);
        Assert.IsNull(runActivity.Comment);
        Assert.AreEqual(Models.RunLog.LogStateValid, runActivity.LogState);
        Assert.AreEqual("29:21", runActivity.TimeTaken);
        Assert.AreEqual(userRoute.Id, runActivity.RouteId);
    }

    [TestMethod]
    public async Task Should_add_new_activity_using_saved_route2()
    {
        var userRoute = await CreateRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/runlog/activity?routeid={userRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        Assert.Contains("""<input type="hidden" name="mapname" value="test-route" />""", activityGetPage);
        Assert.Contains("""<input type="hidden" name="mapdistance" value="6000.0" />""", activityGetPage);
        Assert.Contains($"""<input type="hidden" name="routeid" value="{userRoute.Id}" />""", activityGetPage);
    }

    [TestMethod]
    public async Task Should_add_new_activity_using_new_mapped_route()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync("/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "add", "Add" },
            { "date", "2024-01-20" },
            { "timetaken", "15:10" },
            { "mapname", "new route" },
            { "mapnotes", "just made up" },
            { "mappoints", """[{"lat":50,"lng":0.1},{"lat":50.5,"lng":-1.2}]""" },
            { "mapdistance", "3500" },
            { "distancetype", "4" }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-20", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var runActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(runActivity);
        Assert.IsNull(runActivity.Comment);
        Assert.AreEqual(Models.RunLog.LogStateValid, runActivity.LogState);
        Assert.AreEqual("15:10", runActivity.TimeTaken);
        var runRoute = await context.Route.FindAsync(runActivity.RouteId);
        Assert.IsNotNull(runRoute);
        Assert.AreEqual("new route", runRoute.Name);
        Assert.AreEqual(3500, runRoute.Distance);
        Assert.AreEqual((int)DistanceUnits.Meters, runRoute.DistanceUnits);
        Assert.AreEqual("""[{"lat":50,"lng":0.1},{"lat":50.5,"lng":-1.2}]""", runRoute.MapPoints);
        Assert.AreEqual("just made up", runRoute.Notes);
        Assert.AreEqual(Models.Route.PrivateRoute, runRoute.RouteType);
    }

    private async Task<Route> CreateRouteAsync()
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Name = "test-route",
            RouteType = Route.PrivateRoute,
            Distance = 6000,
            DistanceUnits = (int)DistanceUnits.Meters
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
