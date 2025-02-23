using System.Collections.Concurrent;

namespace RunnersPal.Elevation.Cli;

public class SrtmDownload(string defaultElevationDataDirectory)
{
    private readonly IEnumerable<(string DownloadSource, string Uri)> _downloadSources = [
        ("SRTM_NE_250m_TIF.rar", "https://srtm.csi.cgiar.org/wp-content/uploads/files/250m/SRTM_NE_250m_TIF.rar"),
        ("SRTM_SE_250m_TIF.rar", "https://srtm.csi.cgiar.org/wp-content/uploads/files/250m/SRTM_SE_250m_TIF.rar"),
        ("SRTM_W_250m_TIF.rar", "https://srtm.csi.cgiar.org/wp-content/uploads/files/250m/SRTM_W_250m_TIF.rar")];

    public async Task DownloadAsync()
    {
        Console.Write($"Download to directory [{defaultElevationDataDirectory}]: ");
        var downloadDirectory = Console.ReadLine() ?? "";
        downloadDirectory = string.IsNullOrWhiteSpace(downloadDirectory) ? defaultElevationDataDirectory : downloadDirectory;

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

        ConcurrentDictionary<string, float> sourceProgress = [];

        Console.WriteLine("Downloading:");
        foreach (var (downloadSource, uri) in _downloadSources)
        {
            sourceProgress.TryAdd(downloadSource, 0);
            Console.WriteLine($"[{downloadSource}]: {uri}");
        }

        await using Timer progressTimer = new(
            _ => Console.Write($"\r{(string.Join(", ", sourceProgress.OrderBy(x => x.Key).Select(x => $"[{x.Key}]: {x.Value * 100:0.0}%")))}"),
            null,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500));

        using HttpClient httpClient = new();
        httpClient.Timeout = Timeout.InfiniteTimeSpan;
        await Parallel.ForEachAsync(_downloadSources, async (src, ctx) =>
        {
            Progress<float> progress = new(p => sourceProgress[src.DownloadSource] = p);
            await using FileStream file = new(Path.Combine(downloadDirectory, src.DownloadSource), FileMode.Create, FileAccess.Write, FileShare.None);
            await DownloadToStreamAsync(httpClient, src.Uri, file, progress, ctx);
        });
        progressTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        Console.WriteLine();
        Console.WriteLine("Downloads completed successfully");
    }

    private async Task DownloadToStreamAsync(HttpClient client, string uri, Stream destination, IProgress<float> progress, CancellationToken ctx)
    {
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ctx);
        var contentLength = response.Content.Headers.ContentLength;

        using var download = await response.Content.ReadAsStreamAsync(ctx);

        if (!contentLength.HasValue)
        {
            await download.CopyToAsync(destination, ctx);
            return;
        }

        var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
        await CopyToAsync(download, destination, 81920, relativeProgress, ctx);
        progress.Report(1);
    }

    private async Task CopyToAsync(Stream source, Stream destination, int bufferSize, IProgress<long> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);

        byte[] buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) != 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalBytesRead += bytesRead;
            progress.Report(totalBytesRead);
        }
    }
}