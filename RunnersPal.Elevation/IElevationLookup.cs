namespace RunnersPal.Elevation;

public interface IElevationLookup
{
    public Task<double> LookupAsync(ElevationPoint point);
    public IAsyncEnumerable<double> LookupAsync(IEnumerable<ElevationPoint> points);
}
