using Massive;
using RunnersPal.Core.Data;

namespace RunnersPal.Core.Models
{
    public class SiteSettings : DynamicModel
    {
        public SiteSettings() : base(MassiveDB.ConnectionStringName, "SiteSettings", "Id") { }
    }
}
