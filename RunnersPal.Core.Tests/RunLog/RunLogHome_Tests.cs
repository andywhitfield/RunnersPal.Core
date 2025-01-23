using System.Net;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogHome_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task When_not_logged_on_Should_show_calendar()
    {
        using var client = _webApplicationFactory.CreateClient(false);
        using var response = await client.GetAsync("/runlog");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(responseContent, "Login");
        StringAssert.Contains(responseContent, "<div id=\"calendar\">");
        // as not logged on, should not try and load activities from the api endpoint
        StringAssert.DoesNotMatch(responseContent, new("/api/runlog/activities"));
    }

    [TestMethod]
    public async Task When_logged_on_Should_show_calendar()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/runlog");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(responseContent, "Logout");
        StringAssert.Contains(responseContent, "<div id=\"calendar\">");
        StringAssert.Contains(responseContent, "/api/runlog/activities");
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
