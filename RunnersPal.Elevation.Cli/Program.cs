using RunnersPal.Elevation.Cli;

var defaultElevationDataDirectory = GetElevationDataDirectory();
SrtmDownload srtmDownload = new(defaultElevationDataDirectory);
SrtmExtractor srtmExtractor = new(defaultElevationDataDirectory);
SrtmTiler srtmTiler = new(defaultElevationDataDirectory);
SummaryFile summaryFile = new(defaultElevationDataDirectory);
SrtmElevationLookup lookup = new(defaultElevationDataDirectory);

try
{
    MaxRev.Gdal.Core.GdalBase.ConfigureAll();

    while (await MainMenuLoopAsync()) ;
    Console.WriteLine("Done");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected program error: {ex}");
}

async Task<bool> MainMenuLoopAsync()
{
    Console.WriteLine("""

Elevation Options
=================

1. Download SRTM data
2. Extract SRTM files
3. Create tiles from SRTM files
4. Create/Update summary json file
5. Check elevation

x. Exit

""");

    Console.Write("Option: ");
    var opt = Console.ReadLine() ?? "";
    switch (opt.Trim().ToLowerInvariant())
    {
        case "1":
            await srtmDownload.DownloadAsync();
            break;
        case "2":
            await srtmExtractor.ExtractAsync();
            break;
        case "3":
            srtmTiler.CreateTiles();
            break;
        case "4":
            await summaryFile.CreateAsync();
            break;
        case "5":
            await lookup.GetElevationAsync();
            break;
        case "x":
            return false;
    }

    return true;
}

static string GetElevationDataDirectory()
{
    var solutionDir = TryGetSolutionDirectoryInfo();
    if (solutionDir == null)
        return Directory.GetCurrentDirectory();

    var defaultPath = Path.Combine(solutionDir.FullName, "external", "elevation");
    if (!Directory.Exists(defaultPath))
        Directory.CreateDirectory(defaultPath);
    
    return defaultPath;

    static DirectoryInfo? TryGetSolutionDirectoryInfo(string? currentPath = null)
    {
        var directory = new DirectoryInfo(currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && directory.GetFiles("*.sln").Length == 0)
            directory = directory.Parent;
        return directory;
    }
}
