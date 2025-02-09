using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Elevation;

namespace RunnersPal.Core.Tests.Controllers;

[TestClass]
public class MapControllerTests
{
    private readonly Mock<IElevationLookup> _elevationLookupMock = new();

    private WebApplicationFactoryTest? _webApplicationFactory;

    [TestInitialize]
    public Task InitializeAsync()
    {
        _webApplicationFactory = new(services => services.Replace(ServiceDescriptor.Scoped(_ => _elevationLookupMock.Object)));
        _elevationLookupMock.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<ElevationPoint>>())).Returns(
            new List<double> { 10d, 11d, 9d }.ToAsyncEnumerable());
        return TestStubAuthHandler.AddTestUserAsync(_webApplicationFactory.Services);
    }

    [TestMethod]
    public async Task Should_get_elevation_between_two_points_in_km()
    {
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
