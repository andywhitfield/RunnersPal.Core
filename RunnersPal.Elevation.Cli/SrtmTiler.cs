using System.Text.Json;
using OSGeo.GDAL;

namespace RunnersPal.Elevation.Cli;

public class SrtmTiler(string defaultElevationDataDirectory)
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IEnumerable<(string TifFile, int XTiles, int YTiles)> _tileConfig = [
        ("SRTM_NE_250m.tif", 10, 10),
        ("SRTM_SE_250m.tif", 10, 10),
        ("SRTM_W_250m.tif", 10, 20)
    ];

    public void CreateTiles()
    {
        var defaultElevationDownloadDirectory = Path.Combine(defaultElevationDataDirectory, "download");        
        Console.Write($"Directory containing extracted tif files [{defaultElevationDownloadDirectory}]: ");
        var downloadDirectory = Console.ReadLine() ?? "";
        downloadDirectory = string.IsNullOrWhiteSpace(downloadDirectory) ? defaultElevationDownloadDirectory : downloadDirectory;

        Console.Write($"Directory to create tile files [{defaultElevationDataDirectory}]: ");
        var tilesDirectory = Console.ReadLine() ?? "";
        tilesDirectory = string.IsNullOrWhiteSpace(tilesDirectory) ? defaultElevationDataDirectory : tilesDirectory;

        if (string.IsNullOrWhiteSpace(downloadDirectory))
        {
            Console.WriteLine("No download directory!");
            return;
        }

        if (!Directory.Exists(downloadDirectory))
        {
            Console.WriteLine($"Download directory [{downloadDirectory}] does not exist");
            return;
        }

        if (string.IsNullOrWhiteSpace(tilesDirectory))
        {
            Console.WriteLine("No tiles directory!");
            return;
        }

        if (!Directory.Exists(tilesDirectory))
        {
            Console.WriteLine($"Tiles directory [{tilesDirectory}] does not exist");
            return;
        }

        foreach (var (tifFile, xTiles, yTiles) in _tileConfig)
        {
            var tifFileName = Directory.GetFiles(downloadDirectory, tifFile).FirstOrDefault();
            if (tifFileName == null)
            {
                Console.WriteLine($"Tif file {tifFile} not found, nothing to do.");
                continue;
            }

            Console.WriteLine($"Creating tile from file: {Path.GetFileName(tifFile)}");

            using var ds = Gdal.Open(tifFileName, Access.GA_ReadOnly);
            var gdalInfoJson = Gdal.GDALInfo(ds, new(["-json"]));
            var gdalInfo = JsonSerializer.Deserialize<GdalInfo>(gdalInfoJson, _jsonOptions);
            if (gdalInfo == null)
            {
                Console.WriteLine("Could not parse TIF file");
                continue;
            }

            var xMin = gdalInfo.CornerCoordinates.UpperLeft[0];
            var xSize = gdalInfo.CornerCoordinates.LowerRight[0] - xMin;
            var ySize = gdalInfo.CornerCoordinates.UpperLeft[1] - gdalInfo.CornerCoordinates.LowerRight[1];
            var xDiff = xSize / xTiles;
            Console.WriteLine($"Using coords ({gdalInfo.CornerCoordinates.UpperLeft[0]},{gdalInfo.CornerCoordinates.UpperLeft[1]})-({gdalInfo.CornerCoordinates.LowerRight[0]},{gdalInfo.CornerCoordinates.LowerRight[1]})");
            for (var x = 0; x < xTiles; x++)
            {
                var xMax = xMin + xDiff;
                var yMax = gdalInfo.CornerCoordinates.UpperLeft[1];
                var yDiff = ySize / yTiles;
                for (var y = 0; y < yTiles; y++)
                {
                    var yMin = yMax - yDiff;
                    var destFile = Path.Combine(tilesDirectory, $"{Path.GetFileNameWithoutExtension(tifFile)}_{x}_{y}.tif");
                    Console.WriteLine($"Creating tile: {xMin} {yMax} {xMax} {yMin} [{tifFile}]");
                    Gdal.wrapper_GDALTranslate(
                        destFile,
                        ds,
                        new([$"-projwin", xMin.ToString(), yMax.ToString(), xMax.ToString(), yMin.ToString(), "-of", "GTiff", "-q"]),
                        null,
                        default);
                    yMax = yMin;
                }
                xMin = xMax;
            }
        }

        Console.WriteLine("Successfully created tiles");
    }
}

record GdalInfo(GdalCornerCoordinates CornerCoordinates);
record GdalCornerCoordinates(double[] UpperLeft, double[] LowerLeft, double[] LowerRight, double[] UpperRight);
