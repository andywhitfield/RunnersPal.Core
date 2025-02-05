using RunnersPal.Core.Geolib;

namespace RunnersPal.Core.Services;

public interface IElevationService
{
    Task<IEnumerable<(Coordinate Coordinate, double Distance, double Elevation)>?> CalculateElevationAsync(Coordinate[] coords);
}
