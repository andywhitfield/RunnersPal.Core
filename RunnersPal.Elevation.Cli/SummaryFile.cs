using System.Text.Json;
using OSGeo.GDAL;

namespace RunnersPal.Elevation.Cli;

public class SummaryFile(string defaultElevationDataDirectory)
{
    private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task CreateAsync()
    {
        Console.Write($"Directory containing tif tiles [{defaultElevationDataDirectory}]: ");
        var tilesDirectory = Console.ReadLine() ?? "";
        tilesDirectory = string.IsNullOrWhiteSpace(tilesDirectory) ? defaultElevationDataDirectory : tilesDirectory;

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

        var tifFiles = Directory.GetFiles(tilesDirectory, "*.tif");
        if (tifFiles.Length == 0)
        {
            Console.WriteLine("No tif files found, nothing to do.");
            return;
        }

        List<SummaryItem> summaries = [];
        foreach (var tifFile in tifFiles)
        {
            using var ds = Gdal.Open(tifFile, Access.GA_ReadOnly);
            double[] geoTransform = new double[6];
            ds.GetGeoTransform(geoTransform);
            var lngmin = geoTransform[0];
            var lmax = geoTransform[3];
            var lngmax = lngmin + ds.RasterXSize * geoTransform[1];
            var lmin = lmax + ds.RasterYSize * geoTransform[5];

            summaries.Add(new(Path.GetFileName(tifFile), [lmin, lmax, lngmin, lngmax]));

            Console.WriteLine($"TIF file {Path.GetFileName(tifFile)} has coords ({lmin},{lmax})-({lngmin},{lngmax})");
        }

        await using FileStream stream = new(Path.Combine(tilesDirectory, "summary.json"), FileMode.Create, FileAccess.Write);
        await JsonSerializer.SerializeAsync(stream, summaries, _options);

        Console.WriteLine("Successfully created / updated summary file");
    }
}