using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.User;

[TestClass]
public class StatsTests
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
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-2),
            Route = route1.Entity,
            TimeTaken = "00:42:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-8),
            Route = route2.Entity,
            TimeTaken = "01:30:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-9),
            Route = route2.Entity,
            TimeTaken = "01:31:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddDays(-10),
            Route = route1.Entity,
            TimeTaken = "00:43:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddMonths(-1).AddDays(-1),
            Route = route2.Entity,
            TimeTaken = "01:32:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddMonths(-1).AddDays(-2),
            Route = route1.Entity,
            TimeTaken = "00:44:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddYears(-1).AddDays(-1),
            Route = route2.Entity,
            TimeTaken = "01:33:00",
            UserAccount = user
        });
        context.RunLog.Add(new()
        {
            Date = today.AddYears(-2).AddDays(-1),
            Route = route1.Entity,
            TimeTaken = "00:45:00",
            UserAccount = user
        });

        await context.SaveChangesAsync();
    }

    [TestMethod]
    [DataRow("/user")]
    [DataRow("/user/byweek")]
    public async Task Show_weekly_pace_chart_for_a_user(string uri)
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync(uri);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("['26 Jul','02 Aug','09 Aug','16 Aug','23 Aug']", responseContent);
        Assert.Contains("[30.0,0,0,50.0,30.0]", responseContent);
        Assert.Contains("[4.5,0,0,4.45,4.1]", responseContent);
        Assert.Contains("text: 'Total Distance / Average Pace'", responseContent);
    }

    [TestMethod]
    public async Task Show_monthly_pace_chart_for_a_user()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/user/bymonth");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("['Aug 2025','Sep 2025','Oct 2025','Nov 2025','Dec 2025','Jan 2026','Feb 2026','Mar 2026','Apr 2026','May 2026','Jun 2026','Jul 2026','Aug 2026']", responseContent);
        Assert.Contains("[20.0,0,0,0,0,0,0,0,0,0,0,30.0,80.0]", responseContent);
        Assert.Contains("[4.65,0,0,0,0,0,0,0,0,0,0,4.5,4.28]", responseContent);
        Assert.Contains("text: 'Total Distance / Average Pace'", responseContent);
    }

    [TestMethod]
    public async Task Show_yearly_pace_chart_for_a_user()
    {
        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync("/user/byyear");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("['2024','2025','2026']", responseContent);
        Assert.Contains("[10.0,20.0,110.0]", responseContent);
        Assert.Contains("[4.5,4.65,4.33]", responseContent);
        Assert.Contains("text: 'Total Distance / Average Pace'", responseContent);
    }

    [TestMethod]
    public async Task Show_pace_chart_for_route1()
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        var route = await context.Route.SingleAsync(r => r.Name == "route 1");

        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync($"/user?routeid={route.Id}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("['22 Aug 2026','21 Aug 2026','20 Aug 2026','12 Aug 2026','20 Jul 2026','21 Aug 2024']", responseContent);
        Assert.Contains("[4,4.1,4.2,4.3,4.4,4.5]", responseContent);
        Assert.Contains("text: 'Pace'", responseContent);
    }

    [TestCleanup]
    public void Cleanup() => _webApplicationFactory.Dispose();
}
