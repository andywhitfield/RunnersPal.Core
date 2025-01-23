using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogActivity_Save_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Should_save_manual_distance_activity_successfully()
    {
        var manualRun = await CreateRunLogAsync(new(2024, 1, 23, 0, 0, 0, DateTimeKind.Utc),
            async ctx => ctx.Route.Add(new()
            {
                CreatorAccount = await ctx.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
                Name = "manual distance 1",
                RouteType = Route.PrivateRoute,
                Distance = 3500
            }).Entity,
            "22:55");
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/runlog/activity?activityid={manualRun.Id}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(activityGetPage, $"""<input type="hidden" name="activityid" value="{manualRun.Id}" """);
        StringAssert.Contains(activityGetPage, """<input type="hidden" name="distancetype" value="2" """);
        StringAssert.Contains(activityGetPage, """<input type="hidden" name="distancemanual" value="2.1748" """);
        StringAssert.Contains(activityGetPage, """value="2024-01-23" """);
        
        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "save", "Save" },
            { "activityid", manualRun.Id.ToString() },
            { "date", "2024-01-22" }, // corrected the day
            { "timetaken", manualRun.TimeTaken },
            { "distancetype", "2" },
            { "distancemanual", "2.5" } // changed the manual distance ran
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-22", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var originalRunActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 23, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(originalRunActivity);
        Assert.AreEqual(manualRun.Id, originalRunActivity.Id);
        Assert.AreEqual(Models.RunLog.LogStateDeleted, originalRunActivity.LogState);
        Assert.AreEqual(manualRun.RouteId, originalRunActivity.RouteId);

        var updatedRunActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Date == new DateTime(2024, 1, 22, 0, 0, 0, DateTimeKind.Utc));
        Assert.IsNotNull(updatedRunActivity);
        Assert.AreNotEqual(manualRun.Id, updatedRunActivity.Id);
        Assert.AreNotEqual(manualRun.RouteId, updatedRunActivity.RouteId);
        Assert.AreEqual(manualRun.Id, updatedRunActivity.ReplacesRunLogId);
        var updatedRunRoute = await context.Route.FindAsync(updatedRunActivity.RouteId);
        Assert.IsNotNull(updatedRunRoute);
        Assert.AreEqual("", updatedRunRoute.MapPoints);
        Assert.AreEqual("2.5miles", updatedRunRoute.Name);
        Assert.AreEqual(4023.36m, updatedRunRoute.Distance);
        Assert.AreEqual((int)DistanceUnits.Meters, updatedRunRoute.DistanceUnits);
    }

    private async Task<Models.RunLog> CreateRunLogAsync(DateTime date,
        Func<SqliteDataContext, Task<Route>> getOrCreateRouteFunc, string timeTaken)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var newRun = context.RunLog.Add(new()
        {
            Date = date,
            Route = await getOrCreateRouteFunc(context),
            TimeTaken = timeTaken,
            UserAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
            LogState = Models.RunLog.LogStateValid
        });
        await context.SaveChangesAsync();
        return newRun.Entity;
    }
    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
