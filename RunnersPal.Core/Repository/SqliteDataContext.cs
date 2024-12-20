using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class SqliteDataContext(DbContextOptions<SqliteDataContext> options)
    : DbContext(options), ISqliteDataContext
{
    public void Migrate() => Database.Migrate();
    public DbSet<UserAccount> UserAccount { get; set; } = null!;
    public DbSet<UserAccountAuthentication> UserAccountAuthentication { get; set; } = null!;
    public DbSet<UserPref> UserPref { get; set; } = null!;
    public DbSet<SiteSettings> SiteSettings { get; set; } = null!;
    public DbSet<Models.Route> Route { get; set; } = null!;
    public DbSet<RunLog> RunLog { get; set; } = null!;
}