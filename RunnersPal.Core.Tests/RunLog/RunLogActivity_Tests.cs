using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogActivity_Tests
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

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
