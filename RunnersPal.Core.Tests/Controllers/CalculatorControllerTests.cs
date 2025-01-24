using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class CalculatorControllerTests
{
    private readonly WebApplicationFactoryTest _webApplicationFactory = new();

    [TestInitialize]
    public Task InitializeAsync() => TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);

    [TestMethod]
    [DataRow(null, null, null, false, 0d, 0d)]
    [DataRow(null, null, "km", false, 0d, 0d)]
    [DataRow(null, null, "mile", false, 0d, 0d)]
    [DataRow("", null, null, false, 0d, 0d)]
    [DataRow("", null, "km", false, 0d, 0d)]
    [DataRow("", null, "mile", false, 0d, 0d)]
    [DataRow(null, "", null, false, 0d, 0d)]
    [DataRow(null, "", "km", false, 0d, 0d)]
    [DataRow(null, "", "mile", false, 0d, 0d)]
    [DataRow("", "", null, false, 0d, 0d)]
    [DataRow("", "", "km", false, 0d, 0d)]
    [DataRow("", "", "mile", false, 0d, 0d)]
    [DataRow("0", "", "km", true, 0d, 0d)]
    [DataRow("0", null, "km", true, 0d, 0d)]
    [DataRow("0", "100", "km", true, 0d, 0d)]
    [DataRow("5", null, "km", true, 5d, 3.1069d)]
    [DataRow("5", "10", "km", true, 5d, 3.1069d)]
    [DataRow("5.5", null, "km", true, 5.5d, 3.4175d)]
    [DataRow("-5", null, "km", true, -5d, -3.1069d)]
    [DataRow("", "0", "mile", true, 0d, 0d)]
    [DataRow(null, "0", "mile", true, 0d, 0d)]
    [DataRow("100", "0", "mile", true, 0d, 0d)]
    [DataRow(null, "5", "mile", true, 8.0467d, 5d)]
    [DataRow("10", "5", "mile", true, 8.0467d, 5d)]
    [DataRow(null, "5.5", "mile", true, 8.8514d, 5.5d)]
    [DataRow(null, "-5", "mile", true, -8.0467d, -5d)]
    [DataRow(null, null, "halfmarathon", true, 21.0975d, 13.1094d)]
    [DataRow(null, null, "marathon", true, 42.195d, 26.2188d)]
    public async Task Calculate_distance(string? km, string? mile, string? source, bool expectOk, double expectedKm, double expectedMile)
    {
        using var client = _webApplicationFactory.CreateClient(false); // should allow un-auth user
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/distance", new Dictionary<string, string?>()
        {
            { "km", km }, { "mile", mile }, { "source", source }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DistanceApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(Convert.ToDecimal(expectedKm), result.Km);
        Assert.AreEqual(Convert.ToDecimal(expectedMile), result.Mile);
    }

    [TestMethod]
    [DataRow(null, null, null, false, "", "")]
    [DataRow("a", null, "km", false, "", "")]
    [DataRow("5:30", null, "km", true, "", "8:51")]
    [DataRow(null, "a", "mile", false, "", "")]
    [DataRow(null, "9:20", "mile", true, "5:47", "")]
    public async Task Calculate_pace_conversion(string? km, string? mile, string? source, bool expectOk, string expectedPaceKm, string expectedPaceMile)
    {
        using var client = _webApplicationFactory.CreateClient(false); // should allow un-auth user
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/pace/convert", new Dictionary<string, string?>()
        {
            { "km", km }, { "mile", mile }, { "source", source }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaceAllApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedPaceKm, result.PaceKm);
        Assert.AreEqual(expectedPaceMile, result.PaceMile);
    }

    [TestMethod]
    [DataRow(null, null, null, null, false, 0d, 0d, "", "", "")]
    [DataRow("5", "31:00", null, "pace", true, 0d, 0d, "", "6:12", "9:58")]
    [DataRow("5", null, "6:12", "timetaken", true, 0d, 0d, "31:00", "", "")]
    [DataRow(null, "31:00", "6:12", "distance", true, 5d, 3.1069d, "", "", "")]
    public async Task Calculate_all_pace_conversion(string? distance /* in KM */, string? timeTaken, string? pace, string? dest,
        bool expectOk, double expectedDistanceKm, double expectedDistanceMile, string expectedTimeTaken, string expectedPaceKm, string expectedPaceMile)
    {
        using var client = _webApplicationFactory.CreateClient(false); // should allow un-auth user
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/pace/all", new Dictionary<string, string?>()
        {
            { "distance", distance }, { "timeTaken", timeTaken }, { "pace", pace }, { "dest", dest }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaceAllApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(Convert.ToDecimal(expectedDistanceKm), result.DistanceKm);
        Assert.AreEqual(Convert.ToDecimal(expectedDistanceMile), result.DistanceMile);
        Assert.AreEqual(expectedTimeTaken, result.TimeTaken);
        Assert.AreEqual(expectedPaceKm, result.PaceKm);
        Assert.AreEqual(expectedPaceMile, result.PaceMile);
    }

    [TestMethod]
    [DataRow(null, null, false, 0d)]
    [DataRow("5", "60", true, 310d)]
    public async Task Calculate_calories(string? km, string? weight, bool expectOk, double expectedCalories)
    {
        using var client = _webApplicationFactory.CreateClient(false); // should allow un-auth user
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/calories", new Dictionary<string, string?>()
        {
            { "km", km }, { "weight", weight }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CaloriesApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(Convert.ToDecimal(expectedCalories), result.Calories);
    }

    [TestMethod]
    [DataRow(null, null, null, null, null, false, 0d, 0d, 0d, 0d)]
    [DataRow("lbs", "145", null, null, null, true, 145d, 10d, 5d, 65.7709d)]
    [DataRow("st", null, "10", "5", null, true, 145d, 10d, 5d, 65.7709d)]
    [DataRow("st", null, "10", null, null, false, 0d, 0d, 0d, 0d)]
    [DataRow("stlbs", null, "10", "5", null, true, 145d, 10d, 5d, 65.7709d)]
    [DataRow("stlbs", null, null, "5", null, false, 0d, 0d, 0d, 0d)]
    [DataRow("kg", null, null, null, "65.7709", true, 145d, 10d, 5d, 65.7709d)]
    public async Task Calculate_weight(string? source, string? lbs, string? st, string? stlbs, string? kg,
        bool expectOk, double expectedLbs, double expectedSt, double expectedStlbs, double expectedKg)
    {
        using var client = _webApplicationFactory.CreateClient(false); // should allow un-auth user
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/weight", new Dictionary<string, string?>()
        {
            { "source", source }, { "lbs", lbs }, { "st", st }, { "stlbs", stlbs }, { "kg", kg }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WeightApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(Convert.ToDecimal(expectedLbs), result.Lbs);
        Assert.AreEqual(Convert.ToDecimal(expectedSt), result.St);
        Assert.AreEqual(Convert.ToDecimal(expectedStlbs), result.Stlbs);
        Assert.AreEqual(Convert.ToDecimal(expectedKg), result.Kg);
    }

    [TestMethod]
    public async Task Calculate_pace_requires_authenticated_user()
    {
        using var client = _webApplicationFactory.CreateClient(false);
        using var response = await client.GetAsync("/api/calculator/pace");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    [DataRow(null, null, null, null, null, false, "")]
    [DataRow("26:20", 1, null, "5 Kilometers", null, true, "8:28 min/mile")]
    [DataRow("26:20", 1, null, null, null, false, "")]
    [DataRow("26:20", 2, 4.5d /* miles */, null, null, true, "5:51 min/mile")]
    [DataRow("26:20", 2, null, null, null, false, "")]
    [DataRow("26:20", 2, 0d, null, null, false, "")]
    [DataRow("26:20", 3, null, "route 1", null, true, "1.9miles @ 14:07 min/mile")]
    [DataRow("26:20", 3, null, "other user route 1", null, false, "")]
    [DataRow("26:20", 3, null, "unknown route", null, false, "")]
    [DataRow("26:20", 4, null, null, 6200d, true, "3.9miles @ 6:50 min/mile")]
    [DataRow("26:20", 4, null, null, 0d, false, "")]
    public async Task Calculate_pace(string? timeTaken, int? distanceType, double? distanceManual, string? routeName, double? mapDistance,
        bool expectOk, string expectedPace)
    {
        await using var serviceScope = _webApplicationFactory.Services.CreateAsyncScope();
        await using var context = serviceScope.ServiceProvider.GetRequiredService<SqliteDataContext>();
        await CreateTestRoutesAsync(context);
        var routeId = context.Route.FirstOrDefault(r => r.Name == routeName)?.Id;

        using var client = _webApplicationFactory.CreateClient(true);
        using var response = await client.GetAsync(QueryHelpers.AddQueryString("/api/calculator/pace", new Dictionary<string, string?>()
        {
            { "timeTaken", timeTaken }, { "distanceType", distanceType.ToString() }, { "distanceManual", distanceManual.ToString() },
            { "routeId", routeId.ToString() }, { "mapDistance", mapDistance.ToString() }
        }));
        if (!expectOk)
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            return;
        }

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaceApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedPace, result.Pace);

        static async Task CreateTestRoutesAsync(SqliteDataContext context)
        {
            var otherUserAccount = context.UserAccount.Add(new() { DisplayName = "other user", OriginalHostAddress = "" });
            context.Route.AddRange(
                new() { CreatorAccount = otherUserAccount.Entity, Name = "other user route 1", Distance = 2500, RouteType = Models.Route.PrivateRoute },
                new() { CreatorAccount = await context.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail), Name = "route 1", Distance = 3000, RouteType = Models.Route.PrivateRoute }
            );
            await context.SaveChangesAsync();
        }
    }
}