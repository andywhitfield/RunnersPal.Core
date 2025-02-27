using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace RunnersPal.Elevation.Tests;

[TestClass]
public class ElevationSummaryDataSourceTests
{
    private string? _elevationPath;
    private IConfiguration? _configuration;

    [TestInitialize]
    public void Setup()
    {
        _elevationPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "", "Resources");
        _configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { { "ElevationPath", _elevationPath } })
                    .Build();
    }

    [TestMethod]
    public async Task Should_get_tif_filename_from_summary_json_for_valid_point()
    {
        var latitude = 51.56592793451194d;
        var longitude = -0.1771682768125467d;
        ElevationSummaryDataSource dataSource = new(Mock.Of<ILogger<ElevationSummaryDataSource>>(), _configuration!);
        var tifFile = await dataSource.GetFilenameForPointAsync(new ElevationPoint(latitude, longitude));
        Assert.AreEqual(Path.Combine(_elevationPath!, "SRTM_NE_250m_1_1.tif"), tifFile);
    }

    [TestMethod]
    public async Task Summary_for_zero_zero_throws_due_to_missing_file()
    {
        var latitude = 0d;
        var longitude = 0d;
        ElevationSummaryDataSource dataSource = new(Mock.Of<ILogger<ElevationSummaryDataSource>>(), _configuration!);
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => dataSource.GetFilenameForPointAsync(new ElevationPoint(latitude, longitude)));
        Assert.AreEqual($"TIF file {Path.Combine(_elevationPath!, "SRTM_SE_250m_1_0.tif")} not found. Is the summary file valid?", ex.Message);
    }

    [TestMethod]
    [DataRow(0, -90, "SRTM_W_250m_5_9.tif")]
    [DataRow(0, 90, "SRTM_NE_250m_5_9.tif")]
    [DataRow(-30, -90, "SRTM_W_250m_5_14.tif")]
    [DataRow(-30, 0, "SRTM_SE_250m_1_4.tif")]
    [DataRow(-30, 90, "SRTM_SE_250m_5_4.tif")]
    [DataRow(30, -90, "SRTM_W_250m_5_5.tif")]
    [DataRow(30, 0, "SRTM_NE_250m_1_4.tif")]
    [DataRow(30, 90, "SRTM_NE_250m_5_4.tif")]
    public async Task Summary_for_valid_coords_throws_due_to_missing_file(double latitude, double longitude, string expectedMissingFile)
    {
        ElevationSummaryDataSource dataSource = new(Mock.Of<ILogger<ElevationSummaryDataSource>>(), _configuration!);
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => dataSource.GetFilenameForPointAsync(new ElevationPoint(latitude, longitude)));
        Assert.AreEqual($"TIF file {Path.Combine(_elevationPath!, expectedMissingFile)} not found. Is the summary file valid?", ex.Message);
    }

    [TestMethod]
    [DataRow(-61, 0, "SRTM_SE_250m_1_9.tif")]
    [DataRow(61, 0, "SRTM_NE_250m_1_0.tif")]
    [DataRow(0, -181, "SRTM_W_250m_0_9.tif")]
    [DataRow(0, 181, "SRTM_NE_250m_9_9.tif")]
    public async Task Summary_for_max_min_coords_throws_due_to_missing_file(double latitude, double longitude, string expectedMissingFile)
    {
        ElevationSummaryDataSource dataSource = new(Mock.Of<ILogger<ElevationSummaryDataSource>>(), _configuration!);
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => dataSource.GetFilenameForPointAsync(new ElevationPoint(latitude, longitude)));
        Assert.AreEqual($"TIF file {Path.Combine(_elevationPath!, expectedMissingFile)} not found. Is the summary file valid?", ex.Message);
    }

    [TestMethod]
    [DataRow(-62, 0)]
    [DataRow(62, 0)]
    [DataRow(0, -182)]
    [DataRow(0, 182)]
    public async Task Summary_for_invalid_coords_throws(double latitude, double longitude)
    {
        ElevationSummaryDataSource dataSource = new(Mock.Of<ILogger<ElevationSummaryDataSource>>(), _configuration!);
        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => dataSource.GetFilenameForPointAsync(new ElevationPoint(latitude, longitude)));
        Assert.AreEqual($"Could not get a file for the given point ({new ElevationPoint(latitude, longitude)}). Possibly invalid coordinates provided.", ex.Message);
    }
}