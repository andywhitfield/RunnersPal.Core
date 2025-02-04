namespace RunnersPal.Core.Geolib;

/// <summary>
/// C# port of the three functions I need from the Geolib Javascript library: https://github.com/manuelbieh/geolib
/// </summary>
public class GeoCalculator : IGeoCalculator
{
    public double CalculateDistance(Coordinate from, Coordinate to)
        => Math.Acos(
                RobustAcos(
                    Math.Sin(ToRad(to.Latitude)) * Math.Sin(ToRad(from.Latitude)) +
                        Math.Cos(ToRad(to.Latitude)) *
                        Math.Cos(ToRad(from.Latitude)) *
                        Math.Cos(ToRad(from.Longitude) - ToRad(to.Longitude))
                )
            ) * GeoConstants.EarthRadius;

    public double CalculateBearing(Coordinate origin, Coordinate dest)
    {
        var diffLon = ToRad(dest.Longitude) - ToRad(origin.Longitude);

        // difference latitude coords phi
        var diffPhi = Math.Log(
            Math.Tan(ToRad(dest.Latitude) / 2d + Math.PI / 4d) /
            Math.Tan(ToRad(origin.Latitude) / 2d + Math.PI / 4d)
        );

        // recalculate diffLon if it is greater than pi
        if (Math.Abs(diffLon) > Math.PI)
            diffLon = diffLon > 0 ? (Math.PI * 2d - diffLon) * -1d : Math.PI * 2d + diffLon;

        //return the angle, normalized
        return (ToDeg(Math.Atan2(diffLon, diffPhi)) + 360d) % 360d;
    }

    public Coordinate ComputeDestination(Coordinate start, double distance, double bearing)
    {
        var delta = distance / 6371000d;
        var theta = ToRad(bearing);
        var phi1 = ToRad(start.Latitude);
        var lambda1 = ToRad(start.Longitude);
        var phi2 = Math.Asin(
            Math.Sin(phi1) * Math.Cos(delta) +
                Math.Cos(phi1) * Math.Sin(delta) * Math.Cos(theta)
        );
        var lambda2 = lambda1 +
            Math.Atan2(
                Math.Sin(theta) * Math.Sin(delta) * Math.Cos(phi1),
                Math.Cos(delta) - Math.Sin(phi1) * Math.Sin(phi2)
            );
        var longitude = ToDeg(lambda2);
        if (longitude < GeoConstants.MinLongitude || longitude > GeoConstants.MaxLongitude)
        {
            // normalise to >=-180 and <=180° if value is > MaxLongitude or < MinLongitude
            lambda2 = ((lambda2 + 3d * Math.PI) % (2d * Math.PI)) - Math.PI;
            longitude = ToDeg(lambda2);
        }

        return new(ToDeg(phi2), longitude);
    }

    private static double ToRad(double value)
        => value * Math.PI / 180d;

    private static double ToDeg(double value)
        => value * 180d / Math.PI;

    private static double RobustAcos(double value)
        => value switch
        {
            > 1 => 1,
            < -1 => -1,
            _ => value
        };
}