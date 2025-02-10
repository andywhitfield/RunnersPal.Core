using System.Reflection;
using Microsoft.Extensions.Logging;
using Moq;

namespace RunnersPal.Elevation.Tests;

[TestClass]
public class ElevationLookupTests
{
    [TestMethod]
    [DataRow(51.56592793451194, -0.1771682768125467, 136)]
    [DataRow(51, 0, 28)]
    [DataRow(51, -0.2, 85)]
    [DataRow(52, -0.2, 59)]
    public async Task Lookup_returns_expected_elevation(double latitude, double longitude, double expectedElevation)
    {
        var tifFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "", "Resources", "data", "SRTM_NE_250m_1_1.tif");
        Mock<IElevationSummaryDataSource> elevationSummaryDataSourceMock = new();
        elevationSummaryDataSourceMock.Setup(x => x.GetFilenameForPointAsync(It.IsAny<ElevationPoint>())).ReturnsAsync(tifFile);
        ElevationLookup lookup = new(Mock.Of<ILogger<ElevationLookup>>(), elevationSummaryDataSourceMock.Object);
        var elevation = await lookup.LookupAsync(new ElevationPoint(latitude, longitude));
        Assert.AreEqual(expectedElevation, elevation);
    }

    [TestMethod]
    public async Task Lookup_list_returns_expected_elevations()
    {
        var tifFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "", "Resources", "data", "SRTM_NE_250m_1_1.tif");
        Mock<IElevationSummaryDataSource> elevationSummaryDataSourceMock = new();
        elevationSummaryDataSourceMock.Setup(x => x.GetFilenameForPointAsync(It.IsAny<ElevationPoint>())).ReturnsAsync(tifFile);
        ElevationLookup lookup = new(Mock.Of<ILogger<ElevationLookup>>(), elevationSummaryDataSourceMock.Object);
        var elevations = await lookup.LookupAsync([
            new ElevationPoint(51.56592793451194d, -0.1771682768125467d),
            new ElevationPoint(51, 0),
            new ElevationPoint(51, -0.2),
            new ElevationPoint(52, -0.2)
        ]).ToListAsync();
        Assert.AreEqual(4, elevations.Count);
        Assert.AreEqual(136, elevations[0]);
        Assert.AreEqual(28, elevations[1]);
        Assert.AreEqual(85, elevations[2]);
        Assert.AreEqual(59, elevations[3]);
    }
}