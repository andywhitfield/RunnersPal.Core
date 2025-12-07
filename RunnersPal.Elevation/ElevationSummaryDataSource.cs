using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RunnersPal.Elevation;

public class ElevationSummaryDataSource(
    ILogger<ElevationSummaryDataSource> logger,
    IConfiguration configuration)
    : IElevationSummaryDataSource
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly RTree.RTree<string> _tree = new();
    private string? _path;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private string ElevationPath
    {
        get
        {
            if (_path == null)
            {
                var path = configuration.GetValue("ElevationPath", Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "");
                if (!Directory.Exists(path))
                    throw new InvalidOperationException($"Elevation data directory not found. Configured folder [{path}] does not exist.");

                logger.LogDebug("Reading elevation data from path [{Path}]", path);
                _path = path;
            }
            return _path;
        }
    }

    private async Task<RTree.RTree<string>> GetTreeAsync()
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            if (_tree.Count != 0)
                return _tree;

            var summaryFile = Path.Combine(ElevationPath, "summary.json");
            await using var fileStream = new FileStream(summaryFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var summaries = await JsonSerializer.DeserializeAsync<List<SummaryItem>>(fileStream, _jsonSerializerOptions) ?? throw new InvalidOperationException($"Could not load summaries file [{summaryFile}]");
            logger.LogDebug("Loaded {SummariesCount} summaries", summaries.Count);

            foreach (var summary in summaries)
                _tree.Add(new((float)summary.Coords[0], (float)summary.Coords[2], (float)summary.Coords[1], (float)summary.Coords[3], 0, 0), summary.File);

            return _tree;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<string> GetFilenameForPointAsync(ElevationPoint elevationPoint)
    {
        var nearest = (await GetTreeAsync()).Nearest(new((float)elevationPoint.Latitude, (float)elevationPoint.Longitude, 0), 1);
        var filename = nearest?.FirstOrDefault() ?? throw new InvalidOperationException($"Could not get a file for the given point ({elevationPoint}). Possibly invalid coordinates provided.");

        logger.LogTrace("Got filename [{Filename}] from summary file for point ({ElevationPoint})", filename, elevationPoint);
        filename = Path.Combine(ElevationPath, filename);
        if (!File.Exists(filename))
            throw new InvalidOperationException($"TIF file {filename} not found. Is the summary file valid?");

        return filename;
    }
}
