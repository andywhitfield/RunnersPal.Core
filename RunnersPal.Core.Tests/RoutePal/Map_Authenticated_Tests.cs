using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RoutePal;

[TestClass]
public class Map_Authenticated_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task When_logged_on_Should_show_map()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(responseContent, "Logout");
        StringAssert.Contains(responseContent, "<div id=\"map\">");
        StringAssert.DoesNotMatch(responseContent, new("switch to miles"));
    }

    [TestMethod]
    public async Task Given_routename_and_points_Should_save_new_route()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "routename", "test-route" },
            { "points", """[{"lat":50,"lng":0},{"lat":50,"lng":1}]""" }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRoute = context.Route.SingleOrDefault(r => r.Name == "test-route");
        Assert.IsNotNull(newRoute);
        Assert.AreEqual(new Uri($"/routepal/map?routeid={newRoute.Id}", UriKind.Relative), responsePost.Headers.Location);
        Assert.AreEqual(Route.PrivateRoute, newRoute.RouteType);
        Assert.AreEqual(0, newRoute.Distance);
        Assert.AreEqual((int)DistanceUnits.Meters, newRoute.DistanceUnits);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public async Task When_no_routename_ShouldNot_save(string? routeName)
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        Dictionary<string, string> formParams = new()
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "points", """[{"lat":50,"lng":0},{"lat":50,"lng":1}]""" }
        };
        if (routeName != null)
            formParams["routename"] = routeName;

        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(formParams));
        Assert.AreEqual(HttpStatusCode.BadRequest, responsePost.StatusCode);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public async Task When_no_points_ShouldNot_save(string? points)
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var mapGet = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        Dictionary<string, string> formParams = new()
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) },
            { "routename", "test-route" }
        };
        if (points != null)
            formParams["points"] = points;

        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(formParams));
        Assert.AreEqual(HttpStatusCode.BadRequest, responsePost.StatusCode);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
