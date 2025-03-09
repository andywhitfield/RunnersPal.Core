using Microsoft.Extensions.Logging;
using Moq;
using RunnersPal.Core.Geolib;
using RunnersPal.Core.Services;
using RunnersPal.Elevation;

namespace RunnersPal.Core.Tests.Services;

[TestClass]
public class ElevationServiceTests
{
    [TestMethod]
    public async Task Should_get_elevation_for_two_coords()
    {
        Mock<IGeoCalculator> geoCalculator = new();
        geoCalculator.Setup(x => x.CalculateDistance(new(1, 2), new(1, 3))).Returns(255);

        Mock<IElevationLookup> elevationLookup = new();
        elevationLookup.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<ElevationPoint>>())).Returns(new List<double>([3d, 5d, 4d]).ToAsyncEnumerable());

        ElevationService elevationService = new(new LoggerFactory().CreateLogger<ElevationService>(), geoCalculator.Object, elevationLookup.Object);
        var result = await elevationService.CalculateElevationAsync([new Coordinate(1, 2), new Coordinate(1, 3)]);
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count());
        Assert.AreEqual(3, result.ElementAt(0).Elevation);
        Assert.AreEqual(5, result.ElementAt(1).Elevation);
        Assert.AreEqual(4, result.ElementAt(2).Elevation);
    }

    [TestMethod]
    public async Task Given_too_few_coords_Should_not_calculate()
    {
        Mock<IGeoCalculator> geoCalculator = new();
        geoCalculator.Setup(x => x.CalculateDistance(new(1, 2), new(1, 3))).Returns(255);

        Mock<IElevationLookup> elevationLookup = new();
        elevationLookup.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<ElevationPoint>>())).Returns(new List<double>([3d, 5d, 4d]).ToAsyncEnumerable());

        ElevationService elevationService = new(new LoggerFactory().CreateLogger<ElevationService>(), geoCalculator.Object, elevationLookup.Object);
        Assert.IsNull(await elevationService.CalculateElevationAsync([]));
        Assert.IsNull(await elevationService.CalculateElevationAsync([new Coordinate(1, 2)]));
    }

    [TestMethod]
    public async Task Given_too_many_coords_Should_not_calculate()
    {
        Mock<IGeoCalculator> geoCalculator = new();
        Mock<IElevationLookup> elevationLookup = new();
        elevationLookup.Setup(x => x.LookupAsync(It.IsAny<IEnumerable<ElevationPoint>>())).Returns(new List<double>([3d, 5d]).ToAsyncEnumerable());

        ElevationService elevationService = new(new LoggerFactory().CreateLogger<ElevationService>(), geoCalculator.Object, elevationLookup.Object);
        Assert.IsNull(await elevationService.CalculateElevationAsync([.. Enumerable.Range(1, 8001).Select(x => new Coordinate(x, x))]));
        Assert.IsNotNull(await elevationService.CalculateElevationAsync([.. Enumerable.Range(1, 8000).Select(x => new Coordinate(x, x))]));
    }
}