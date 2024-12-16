using Microsoft.EntityFrameworkCore;

namespace RunnersPal.Core.Repository;

public class SqliteDataContext(DbContextOptions<SqliteDataContext> options) : DbContext(options), ISqliteDataContext
{
    public void Migrate() => Database.Migrate();
}