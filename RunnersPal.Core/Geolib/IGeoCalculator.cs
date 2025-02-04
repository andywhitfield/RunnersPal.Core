namespace RunnersPal.Core.Geolib;

public interface IGeoCalculator
{
    double CalculateBearing(Coordinate origin, Coordinate dest);
    double CalculateDistance(Coordinate from, Coordinate to);
    Coordinate ComputeDestination(Coordinate start, double distance, double bearing);
}
