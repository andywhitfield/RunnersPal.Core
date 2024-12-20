
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RunnersPal.Core.Repository;

// used by the migrations tool only
public class SqliteDataContextFactory : IDesignTimeDbContextFactory<SqliteDataContext>
{
    public SqliteDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDataContext>();
        optionsBuilder.UseSqlite("Data Source=RunnersPal.Core/runnerspal.db");
        return new SqliteDataContext(optionsBuilder.Options);
    }
}
