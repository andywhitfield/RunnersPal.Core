using RunnersPal.Core.Geolib;

namespace RunnersPal.Core.Services;

public interface IOpenElevationClient
{
    Task<OpenElevationResponseModel?> LookupAsync(IEnumerable<Coordinate> coords);
}
