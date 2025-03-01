using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RunnersPal.Elevation.Cli;

public class SrtmElevationLookup(string defaultElevationDataDirectory)
{
    public async Task GetElevationAsync()
    {
        Console.Write($"Tiles directory [{defaultElevationDataDirectory}]: ");
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

        LoggerFactory loggerFactory = new();
        ConfigurationBuilder config = new();
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ElevationPath", tilesDirectory}
        });
        ElevationSummaryDataSource elevationSummaryDataSource = new(loggerFactory.CreateLogger<ElevationSummaryDataSource>(), config.Build());
        ElevationLookup lookup = new(loggerFactory.CreateLogger<ElevationLookup>(), elevationSummaryDataSource);

        Console.Write("Latitude: ");
        var val = Console.ReadLine();
        if (!double.TryParse(val, out var lat))
        {
            Console.WriteLine("Not a valid latitude");
            return;
        }
        Console.Write("Longitude: ");
        val = Console.ReadLine();
        if (!double.TryParse(val, out var lng))
        {
            Console.WriteLine("Not a valid longitude");
            return;
        }
        Console.WriteLine($"Elevation at ({lat},{lng})={await lookup.LookupAsync(new ElevationPoint(lat, lng))}");
    }
}