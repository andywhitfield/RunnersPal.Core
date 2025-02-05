using RunnersPal.Core.Geolib;

namespace RunnersPal.Core.Controllers.ApiModels;

public record LatLngApiModel(decimal Lat, decimal Lng)
{
    public static implicit operator Coordinate(LatLngApiModel ll) => new(Convert.ToDouble(ll.Lat), Convert.ToDouble(ll.Lng));
}
