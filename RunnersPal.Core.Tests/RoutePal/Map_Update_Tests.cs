using System.Net;
using System.Text.Encodings.Web;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RoutePal;

[TestClass]
public class Map_Update_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Should_load_test_route()
    {
        var testRoute = await CreateTestRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        StringAssert.Contains(mapGetPage, $"""<input type="hidden" name="points" value="{HtmlEncoder.Default.Encode(testRoute.MapPoints ?? "")}" />""");
        StringAssert.Contains(mapGetPage, """<input type="hidden" name="distance" value="1600.0" />""");
        StringAssert.Contains(mapGetPage, $"""<input type="hidden" name="routeid" value="{testRoute.Id}" />""");
    }

    [TestMethod]
    public async Task Given_test_route_When_updating_name_and_notes_Should_save_successfully()
    {
        var testRoute = await CreateTestRouteAsync();
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "save", "Save" },
            { "routename", "test-route-updated" },
            { "routenotes", "new route notes" },
            { "distance", "1600.0" },
            { "routeid", testRoute.Id.ToString() },
            { "points", HtmlEncoder.Default.Encode(testRoute.MapPoints ?? "") }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);

        // the update should mark the previous route as deleted and created a new route
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var originalRoute = context.Route.SingleOrDefault(r => r.Name == testRoute.Name);
        var updatedRoute = context.Route.SingleOrDefault(r => r.Name == "test-route-updated");
        Assert.IsNotNull(originalRoute);
        Assert.IsNotNull(updatedRoute);
        Assert.AreEqual(new Uri($"/routepal/map?routeid={updatedRoute.Id}", UriKind.Relative), responsePost.Headers.Location);
    }

    [TestMethod]
    public async Task Given_a_deleted_route_When_updating_Should_not_save()
    {
        var testRoute = await CreateTestRouteAsync();
        async Task DeleteRouteAsync()
        {
            await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
            await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var route = await context.Route.FindAsync(testRoute.Id);
            Assert.IsNotNull(route);
            route.RouteType = Route.DeletedRoute;
            await context.SaveChangesAsync();
        }
        await DeleteRouteAsync();

        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "save", "Save" },
            { "routename", "test-route-updated" },
            { "routenotes", "new route notes" },
            { "distance", "1600.0" },
            { "routeid", testRoute.Id.ToString() },
            { "points", HtmlEncoder.Default.Encode(testRoute.MapPoints ?? "") }
        }));
        Assert.AreEqual(HttpStatusCode.BadRequest, responsePost.StatusCode);
    }

    [TestMethod]
    public async Task Given_an_active_route_Should_be_able_to_delete()
    {
        var testRoute = await CreateTestRouteAsync();

        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "delete", "delete" },
            { "routename", testRoute.Name },
            { "distance", "1600.0" },
            { "routeid", testRoute.Id.ToString() },
            { "points", HtmlEncoder.Default.Encode(testRoute.MapPoints ?? "") }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/routepal", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRoute = context.Route.SingleOrDefault(r => r.Name == testRoute.Name);
        Assert.IsNotNull(updatedRoute);
        Assert.AreEqual(Route.DeletedRoute, updatedRoute.RouteType);
    }

    [TestMethod]
    public async Task Cannot_load_route_not_created_by_logged_in_user()
    {
        async Task<Route> CreateRouteByAnotherUserAsync()
        {
            await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
            await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var userAccount = context.UserAccount.Add(new() { DisplayName = "another user", OriginalHostAddress = "", EmailAddress = "another-user-email" });
            var route = context.Route.Add(new() { CreatorAccount = userAccount.Entity, Name = "test-route-by-another-user" });
            await context.SaveChangesAsync();
            return route.Entity;
        }
        var testRoute = await CreateRouteByAnotherUserAsync();

        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.BadRequest, mapGet.StatusCode);
    }

    [TestMethod]
    public async Task Cannot_save_route_not_created_by_logged_in_user()
    {
        async Task<Route> CreateRouteByAnotherUserAsync()
        {
            await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
            await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var userAccount = context.UserAccount.Add(new() { DisplayName = "another user", OriginalHostAddress = "", EmailAddress = "another-user-email" });
            var route = context.Route.Add(new() { CreatorAccount = userAccount.Entity, Name = "test-route-by-another-user" });
            await context.SaveChangesAsync();
            return route.Entity;
        }
        var testRouteNotOwned = await CreateRouteByAnotherUserAsync();

        // first, load the page using our route
        var testRoute = await CreateTestRouteAsync();

        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync($"/routepal/map?routeid={testRoute.Id}");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();

        // then attempt to update using the routeid of the route which isn't ours
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "delete", "delete" },
            { "routename", testRoute.Name },
            { "distance", "1600.0" },
            { "routeid", testRouteNotOwned.Id.ToString() },
            { "points", HtmlEncoder.Default.Encode(testRoute.MapPoints ?? "") }
        }));
        Assert.AreEqual(HttpStatusCode.BadRequest, responsePost.StatusCode);
    }

    private async Task<Route> CreateTestRouteAsync()
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.Add(new()
        {
            Name = "test-route",
            MapPoints = "[{'lat':50,'lng':1}]",
            CreatorAccount = context.UserAccount.Single(u => u.EmailAddress == TestStubAuthHandler.TestUserEmail),
            Distance = 1600,
            DistanceUnits = (int)DistanceUnits.Meters,
            RouteType = Route.PrivateRoute
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
