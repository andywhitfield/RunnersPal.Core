using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.User;

[TestClass]
public class RunLogTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public async Task InitializeAsync()
    {
        await TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var user = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail);
        user.DistanceUnits = (int)DistanceUnits.Kilometers;

        var route1 = context.Route.Add(new()
        {
            Name = "route 1",
            RouteType = Route.PrivateRoute,
            CreatorAccount = user,
            MapPoints = "[some-points-for-route-1]",
            Distance = 10000,
            DistanceUnits = (int)DistanceUnits.Meters
        });
        var route2 = context.Route.Add(new()
        {
            Name = "route 2",
            RouteType = Route.PrivateRoute,
            CreatorAccount = user,
            MapPoints = "[some-points-for-route-2]",
            Distance = 20000,
            DistanceUnits = (int)DistanceUnits.Meters
        });

        var otherUser = context.UserAccount.Add(new() { DisplayName = "Test user 2", OriginalHostAddress = "Test user 2 host" });
        var route3 = context.Route.Add(new()
        {
            Name = "route 3",
            RouteType = Route.PrivateRoute,
            CreatorAccount = otherUser.Entity,
            MapPoints = "[some-points-for-route-3]",
            Distance = 30000,
            DistanceUnits = (int)DistanceUnits.Meters
        });

        DateTime today = new(2026, 8, 22);
        context.RunLog.Add(new()
        {
            Date = today,
            Route = route1.Entity,
            TimeTaken = "00:40:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-1),
            Route = route1.Entity,
            TimeTaken = "00:41:00",
            UserAccount = user
        });

        // add an entry for another user which should not be included in the csv download
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-2),
            Route = route3.Entity,
            TimeTaken = "00:42:00",
            UserAccount = otherUser.Entity
        });

        context.RunLog.Add(new()
        {
            Date = today.AddDays(-3),
            Route = route2.Entity,
            TimeTaken = "01:30:00",
            UserAccount = user
        });

        await context.SaveChangesAsync();
    }

    [TestMethod]
    public async Task Should_download_runlog_for_test_user()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/user/runlog/download");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();

        var lines = responseContent.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.HasCount(4, lines);
        Assert.AreEqual("Date,Distance,Time,Pace,RouteName,Comment", lines[0]);
        Assert.AreEqual("2026-08-19,20,01:30:00,04:30,route 2,", lines[1]);
        Assert.AreEqual("2026-08-21,10,00:41:00,04:06,route 1,", lines[2]);
        Assert.AreEqual("2026-08-22,10,00:40:00,04:00,route 1,", lines[3]);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
