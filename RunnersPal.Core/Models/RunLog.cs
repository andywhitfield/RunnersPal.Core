using Massive;
using RunnersPal.Core.Data;

namespace RunnersPal.Core.Models
{
    public class RunLog : DynamicModel
    {
        public RunLog() : base(MassiveDB.ConnectionStringName, "RunLog", "Id", primaryKeyFieldSequence: "Id", connectionStringProvider: MassiveDB.ConnectionStringProvider) { }
    }

    public static class RunLogExtensions
    {
        public static dynamic Route(this object runLog)
        {
            dynamic dynRunLog = runLog as dynamic;
            return new Route().Single(dynRunLog.RouteId);
        }
    }
}
