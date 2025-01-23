using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.RunLog;

[TestClass]
public class RunLogActivity_Delete_Tests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    public async Task Should_delete_activity_successfully()
    {
        var run = await CreateRunLogAsync(new(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
            ctx => ctx.Route.SingleAsync(r => r.Name == "Half-marathon" && r.RouteType == Route.SystemRoute),
            "1:59:58");
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/runlog/activity?activityid={run.Id}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var activityGetPage = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(activityGetPage, $"""<input type="hidden" name="activityid" value="{run.Id}" """);
        StringAssert.Contains(activityGetPage, """<input type="hidden" name="distancetype" value="1" """);
        StringAssert.Contains(activityGetPage, $"""<input type="hidden" name="routeid" value="{run.RouteId}" """);
        StringAssert.Contains(activityGetPage, """value="2024-01-13" """);

        using var responsePost = await client.PostAsync("/runlog/activity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "__RequestVerificationToken", WebApplicationFactoryTest.GetFormValidationToken(activityGetPage) },
            { "delete", "Delete" },
            { "date", "2024-01-13" },
            { "activityid", run.Id.ToString() }
        }));
        Assert.AreEqual(HttpStatusCode.Redirect, responsePost.StatusCode);
        Assert.AreEqual(new Uri($"/runlog?date=2024-01-13", UriKind.Relative), responsePost.Headers.Location);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var updatedRunActivity = await context.RunLog.SingleOrDefaultAsync(x => x.Id == run.Id);
        Assert.IsNotNull(updatedRunActivity);
        Assert.AreEqual(Models.RunLog.LogStateDeleted, updatedRunActivity.LogState);
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
