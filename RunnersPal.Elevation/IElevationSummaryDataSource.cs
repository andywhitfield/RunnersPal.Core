namespace RunnersPal.Elevation;

public interface IElevationSummaryDataSource
{
    Task<string> GetFilenameForPointAsync(ElevationPoint elevationPoint);
}
