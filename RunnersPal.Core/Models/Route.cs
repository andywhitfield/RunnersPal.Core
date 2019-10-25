using System;
using Massive;
using RunnersPal.Core.Data;

namespace RunnersPal.Core.Models
{
    public class Route : DynamicModel
    {
        public static readonly string PublicRoute = "P";
        public static readonly string PrivateRoute = "M";
        public static readonly string SystemRoute = "Z";
        public static readonly string DeletedRoute = "D";

        public Route() : base(MassiveDB.ConnectionStringName, "Route", "Id", primaryKeyFieldSequence: "Id", connectionStringProvider: MassiveDB.ConnectionStringProvider) { }
    }

    public static class RouteExtensions
    {
        public static Distance Distance(this object route)
        {
            var dynRoute = route as dynamic;
            var distanceUnitInt = (int)dynRoute.DistanceUnits;
            var distanceUnit = DistanceUnits.Miles;
            if (Enum.IsDefined(typeof(DistanceUnits), distanceUnitInt))
                distanceUnit = (DistanceUnits)distanceUnitInt;

            return new Distance((double)dynRoute.Distance, distanceUnit);
        }
    }
}
