using Massive;
using RunnersPal.Core.Data;

namespace RunnersPal.Core.Models
{
    public class UserAccountAuthentication : DynamicModel
    {
        public UserAccountAuthentication() : base(MassiveDB.ConnectionStringName, "UserAccountAuthentication", "Id", primaryKeyFieldSequence: "Id", connectionStringProvider: MassiveDB.ConnectionStringProvider) { }
    }
}
