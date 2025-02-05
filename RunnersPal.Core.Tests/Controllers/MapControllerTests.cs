using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Geolib;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class MapControllerTests
{
    private readonly Mock<IOpenElevationClient> _openElevationClientMock = new();

    private WebApplicationFactoryTest? _webApplicationFactory;

    [TestInitialize]
    public Task InitializeAsync()
    {
        _webApplicationFactory = new(services => services.Replace(ServiceDescriptor.Scoped(_ => _openElevationClientMock.Object)));
        return TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);
    }

    [TestMethod]
    public async Task Should_get_elevation_between_two_points_in_km()
    {
        _openElevationClientMock.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<Coordinate>>())).ReturnsAsync(
            new OpenElevationResponseModel([new(50, 0, 10), new(50, 0.002, 11), new(50, 0.004, 9)]));
        using var client = _webApplicationFactory!.CreateClient(false); // should allow un-auth user
        using var response = await client.PostAsync("/api/map/elevation", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "points", """[{"lat":50,"lng":0},{"lat":50,"lng":0.004}]""" }
        }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ElevationApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual("0.0,0.3,0.3", string.Join(',', result.Series));
        Assert.AreEqual("10,11,9", string.Join(',', result.Elevation.Select(e => e.ToString("0"))));
    }

    [TestMethod]
    public async Task Should_get_elevation_between_two_points_in_miles()
    {
        _openElevationClientMock.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<Coordinate>>())).ReturnsAsync(
            new OpenElevationResponseModel([new(50, 0, 10), new(50, 0.002, 11), new(50, 0.004, 9)]));
        using var client = _webApplicationFactory!.CreateClient(false); // should allow un-auth user
        using var response = await client.PostAsync("/api/map/elevation", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "points", """[{"lat":50,"lng":0},{"lat":50,"lng":0.004}]""" },
            { "unit", "miles" }
        }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ElevationApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual("0.0,0.2,0.2", string.Join(',', result.Series));
        Assert.AreEqual("10,11,9", string.Join(',', result.Elevation.Select(e => e.ToString("0"))));
    }

    [TestMethod]
    [DataRow("", false)]
    [DataRow("Kilometers", true)]
    [DataRow("Miles", false)]
    public async Task Given_logged_in_user_Should_get_elevation_between_two_points_in_user_units(string userUnits, bool expectDistanceInKm)
    {
        _openElevationClientMock.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<Coordinate>>())).ReturnsAsync(
            new OpenElevationResponseModel([new(50, 0, 10), new(50, 0.002, 11), new(50, 0.004, 9)]));
        
        if (!string.IsNullOrEmpty(userUnits))
        {
            await using var ctx = _webApplicationFactory!.Services.CreateAsyncScope();
            var dbContext = ctx.ServiceProvider.GetRequiredService<SqliteDataContext>();
            var testUser = await dbContext.UserAccount.SingleAsync(ua => ua.EmailAddress == TestStubAuthHandler.TestUserEmail);
            testUser.DistanceUnits = (int)Enum.Parse<DistanceUnits>(userUnits);
            await dbContext.SaveChangesAsync();
        }

        using var client = _webApplicationFactory!.CreateClient(true);
        using var response = await client.PostAsync("/api/map/elevation", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "points", """[{"lat":50,"lng":0},{"lat":50,"lng":0.004}]""" }
        }));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ElevationApiModel>();
        Assert.IsNotNull(result);
        Assert.AreEqual(expectDistanceInKm ? "0.0,0.3,0.3" : "0.0,0.2,0.2", string.Join(',', result.Series));
        Assert.AreEqual("10,11,9", string.Join(',', result.Elevation.Select(e => e.ToString("0"))));
    }
}
