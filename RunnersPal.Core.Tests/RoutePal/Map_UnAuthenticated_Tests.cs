using System.Net;

namespace RunnersPal.Core.Tests.RoutePal;

[TestClass]
public class Map_UnAuthenticated_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task When_not_logged_on_Should_show_map()
    {
        using var client = _webApplicationFactory.CreateClient(false);
        using var response = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(responseContent, "Login");
        StringAssert.Contains(responseContent, "<div id=\"map\">");
        StringAssert.Contains(responseContent, "switch to miles");
    }

    [TestMethod]
    public async Task When_not_logged_on_Should_not_load_route()
    {
        using var client = _webApplicationFactory.CreateClient(false, false);
        using var response = await client.GetAsync("/routepal/map?routeid=1");
        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual(new Uri($"/signin?ReturnUrl={WebUtility.UrlEncode("/routepal/map?routeid=1")}", UriKind.Relative), response.Headers.Location);
    }

    [TestMethod]
    public async Task Given_not_logged_on_When_saving_route_Should_redirect_to_login()
    {
        using var client = _webApplicationFactory.CreateClient(false, false);
        using var mapGet = await client.GetAsync("/routepal/map");
        Assert.AreEqual(HttpStatusCode.OK, mapGet.StatusCode);
        var mapGetPage = await mapGet.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/routepal/map", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(mapGetPage) }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/signin?ReturnUrl={WebUtility.UrlEncode("/routepal/map?loadunsaved=true")}", UriKind.Relative), responsePost.Headers.Location);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
