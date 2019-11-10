using System;
using Massive;
using RunnersPal.Core.Data;
using RunnersPal.Core.Data.Caching;

namespace RunnersPal.Core.Models
{
    public class RunLog : DynamicModel
    {
        public RunLog() : base(MassiveDB.ConnectionStringName, "RunLog", "Id", primaryKeyFieldSequence: "Id", connectionStringProvider: MassiveDB.ConnectionStringProvider) { }
    }

    public static class RunLogExtensions
    {
        public static dynamic Route(this object runLog, IDataCache cache = null)
        {
            dynamic dynRunLog = runLog as dynamic;
            var routeId = (long)dynRunLog.RouteId;
            Func<dynamic> getRouteFunc = () => new Route().Single(routeId);
            return cache == null ? getRouteFunc() : cache.Get($"route.{routeId}", getRouteFunc);
        }
    }
}
