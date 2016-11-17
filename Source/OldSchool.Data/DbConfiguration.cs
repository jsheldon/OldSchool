using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;

namespace OldSchool.Data
{
    public class SqlServerConfiguration : DbConfiguration
    {
        public SqlServerConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy());
            SetDefaultConnectionFactory(new LocalDbConnectionFactory("v11.0"));
        }
    }
}