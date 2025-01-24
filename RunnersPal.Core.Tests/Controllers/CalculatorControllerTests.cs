using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RunnersPal.Core.Controllers.ApiModels;

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
        Assert.AreEqual(Convert.ToDecimal(expectedLbs), result.Lbs, JsonSerializer.Serialize(result));
        Assert.AreEqual(Convert.ToDecimal(expectedSt), result.St);
        Assert.AreEqual(Convert.ToDecimal(expectedStlbs), result.Stlbs);
        Assert.AreEqual(Convert.ToDecimal(expectedKg), result.Kg);
    }
}