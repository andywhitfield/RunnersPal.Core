using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class RunLogControllerTests
{
    private readonly Random _random = new();
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public async Task InitializeAsync()
    {
        await TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);
        await CreateRunLogAsync(new(2024, 12, 30, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 2, 11, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 3, 31, 0, 0, 0, DateTimeKind.Utc));
        await CreateRunLogAsync(new(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    [DataRow("2024-10-01", "")]
    [DataRow("2024-10-31", "")]
    [DataRow("2024-11-01", "2024-12-30")]
    [DataRow("2024-11-30", "2024-12-30")]
    [DataRow("2024-12-01", "2024-12-30,2025-01-01,2025-01-05,2025-01-20")]
    [DataRow("2024-12-31", "2024-12-30,2025-01-01,2025-01-05,2025-01-20")]
    [DataRow("2025-01-01", "2024-12-30,2025-01-01,2025-01-05,2025-01-20,2025-02-01,2025-02-11")]
    [DataRow("2025-01-31", "2024-12-30,2025-01-01,2025-01-05,2025-01-20,2025-02-01,2025-02-11")]
    [DataRow("2025-02-01", "2025-01-01,2025-01-05,2025-01-20,2025-02-01,2025-02-11,2025-03-31")]
    [DataRow("2025-02-28", "2025-01-01,2025-01-05,2025-01-20,2025-02-01,2025-02-11,2025-03-31")]
    [DataRow("2025-03-01", "2025-02-01,2025-02-11,2025-03-31,2025-04-01")]
    [DataRow("2025-03-31", "2025-02-01,2025-02-11,2025-03-31,2025-04-01")]
    [DataRow("2025-04-01", "2025-03-31,2025-04-01")]
    [DataRow("2025-04-30", "2025-03-31,2025-04-01")]
    [DataRow("2025-05-01", "2025-04-01")]
    [DataRow("2025-05-31", "2025-04-01")]
    [DataRow("2025-06-01", "")]
    [DataRow("2025-06-30", "")]
    public async Task Should_return_previous_and_current_and_next_month_activities(string requestDate, string expectedRunEvents)
    {
        using var client = _webApplicationFactory.CreateClient(true, false);
        using var response = await client.GetAsync($"/api/runlog/activities?date={requestDate}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var runLogEvents = await response.Content.ReadFromJsonAsync<RunLogEventApiModel[]>();
        Assert.IsNotNull(runLogEvents);
        var expectedRunEventList = expectedRunEvents.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(expectedRunEventList.Length, runLogEvents.Length);
        foreach (var expectedRunEvent in expectedRunEventList)
            Assert.IsTrue(runLogEvents.Any(r => r.Date.ToString("yyyy-MM-dd") == expectedRunEvent));
    }

    private async Task CreateRunLogAsync(DateTime date)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        context.RunLog.Add(new()
        {
            Date = date,
            UserAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail),
            TimeTaken = "10:00",
            Route = await context.Route.ElementAtAsync(_random.Next(0, await context.Route.CountAsync())),
            LogState = Models.RunLog.LogStateValid
        });
        await context.SaveChangesAsync();
    }
}