using SharpCompress.Archives;
using SharpCompress.Archives.Rar;

namespace RunnersPal.Elevation.Cli;

public class SrtmExtractor(string defaultElevationDataDirectory)
{
    public async Task ExtractAsync()
    {
        var defaultElevationDownloadDirectory = Path.Combine(defaultElevationDataDirectory, "download");
        Console.Write($"Directory containing download rar files [{defaultElevationDownloadDirectory}]: ");
        var downloadDirectory = Console.ReadLine() ?? "";
        downloadDirectory = string.IsNullOrWhiteSpace(downloadDirectory) ? defaultElevationDownloadDirectory : downloadDirectory;

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

        var rarFiles = Directory.GetFiles(downloadDirectory, "*.rar");
        if (rarFiles.Length == 0)
        {
            Console.WriteLine("No rar files found, nothing to do.");
            return;
        }

        foreach (var rarFile in rarFiles)
        {
            Console.WriteLine($"Extracting rar file: {Path.GetFileName(rarFile)}");
            await ExtractRarAsync(rarFile, downloadDirectory);
        }

        Console.WriteLine("Successfully extracted rar files");
    }

    static async Task ExtractRarAsync(string rarFile, string destinationDirectory)
    {
        await using var archive = RarArchive.OpenAsyncArchive(rarFile, new() { ExtractFullPath = true, Overwrite = true });
        await foreach (var entry in archive.EntriesAsync.Where(entry => !entry.IsDirectory && (entry.Key?.EndsWith(".tif") ?? false)))
        {
            var extractedFile = Path.Combine(destinationDirectory, entry.Key!);
            Console.WriteLine($"Extracting rar entry [{entry.Key}] to [{extractedFile}]");
            await entry.WriteToFileAsync(extractedFile);
        }
    }
}