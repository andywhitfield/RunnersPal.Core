using RunnersPal.Core.Geolib;
using RunnersPal.Elevation;

namespace RunnersPal.Core.Services;

public class ElevationService(
    ILogger<ElevationService> logger,
    IGeoCalculator geoCalculator,
    IElevationLookup elevationLookup)
    : IElevationService
{
    private const double _elevationFrequency = 125d;
    private const int _maxCoords = 8000;

    public async Task<IEnumerable<(Coordinate Coordinate, double Distance, double Elevation)>?> CalculateElevationAsync(Coordinate[] coords)
    {
        if (coords.Length < 2)
        {
            logger.LogWarning("Less than two coords, not generating elevation for: {CoordsLength}", coords.Length);
            return null;
        }
        if (coords.Length > _maxCoords)
        {
            logger.LogWarning("Too many coords, cannot generate elevation for: {CoordsLength}", coords.Length);
            return null;
        }

        List<(Coordinate Coordinate, double Distance)> elevationCoords = [(coords[0], 0d)];
        var distance = 0d;
        for (var i = 1; i < coords.Length; i++)
        {
            Coordinate lastPoint = coords[i - 1];
            var bearing = geoCalculator.CalculateBearing(lastPoint, coords[i]);
            var currentDistance = distance;
            distance += geoCalculator.CalculateDistance(lastPoint, coords[i]);
            while (distance >= (elevationCoords.Count + 1) * _elevationFrequency)
            {
                var distanceToNextCoord = ((elevationCoords.Count + 1) * _elevationFrequency) - currentDistance;
                currentDistance += distanceToNextCoord;
                var nextCoord = geoCalculator.ComputeDestination(lastPoint, distanceToNextCoord, bearing);
                elevationCoords.Add((nextCoord, currentDistance));
                lastPoint = nextCoord;
            }
        }
        elevationCoords.Add((coords[^1], distance));
        logger.LogDebug("Looking up elevation for coords: [{Coords}]", elevationCoords.Select(x => $"{x.Coordinate}|{x.Distance}"));

        if (elevationCoords.Count > _maxCoords)
        {
            logger.LogWarning("Too many elevation coords to calculate, cannot generate elevation for: {ElevationCoordsCount}", elevationCoords.Count);
            return null;
        }

        var elevations = await elevationLookup.LookupAsync(elevationCoords.Select(c => new ElevationPoint(c.Coordinate.Latitude, c.Coordinate.Longitude))).ToListAsync();
        logger.LogDebug("Got elevation results: [{Elevations}]", elevations);
        if (elevations.Count != elevationCoords.Count)
        {
            logger.LogWarning("Did not get expected number of elevation responses: expected {Expected} vs actual {Actual}", elevationCoords.Count, elevations.Count);
            return null;
        }

        return elevationCoords.Zip(elevations).Select(r => (r.First.Coordinate, r.First.Distance, r.Second));
    }
}
