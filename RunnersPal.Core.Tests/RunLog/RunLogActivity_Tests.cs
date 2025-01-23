using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogActivity_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task When_cancelling_add_run_activity_Should_redirect_and_add_no_entry()
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "cancel", "Cancel" },
            { "date", "2024-01-23" },
            { "timetaken", "1:02:45" },
            { "distancetype", "2" },
            { "distancemanual", "5" }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-23", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        Assert.AreEqual(0, await context.RunLog.CountAsync());
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public async Task Given_no_manual_distance_When_adding_new_activity_Should_return_bad_request(string? distanceManual)
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/runlog/activity");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        Dictionary<string, string> formParams = new()
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "add", "Add" },
            { "date", "2024-01-23" },
            { "timetaken", "01:00" },
            { "distancetype", "2" }
        };
        if (distanceManual != null)
            formParams["distancemanual"] = distanceManual;
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(formParams));
        Assert.AreEqual(HttpStatusCode.BadRequest, responsePost.StatusCode);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
