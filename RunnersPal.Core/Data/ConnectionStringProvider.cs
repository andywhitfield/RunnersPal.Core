using Massive;
using Microsoft.Extensions.Configuration;

namespace RunnersPal.Core.Data
{
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IConfiguration configuration;

        public ConnectionStringProvider(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string GetConnectionString(string connectionStringName)
        {
            return configuration.GetConnectionString(connectionStringName);
        }

        public string GetProviderName(string connectionStringName)
        {
            return null;
        }
    }
}