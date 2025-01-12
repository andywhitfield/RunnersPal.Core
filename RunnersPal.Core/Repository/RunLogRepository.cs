using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class RunLogRepository(ILogger<RunLogRepository> logger, SqliteDataContext context)
    : IRunLogRepository
{
    public Task<RunLog?> GetRunLogAsync(int id) => context.RunLog.Include(r => r.Route).SingleOrDefaultAsync(r => r.Id == id);

    public Task CreateNewAsync(UserAccount userAccount, DateTime runDate, Models.Route route, string timeTaken, string? comment)
    {
        logger.LogDebug("Creating new run log for [{User}]", userAccount.Id);
        context.RunLog.Add(new()
        {
            UserAccount = userAccount,
            Route = route,
            Date = runDate,
            TimeTaken = timeTaken,
            Comment = comment,
            CreatedDate = DateTime.UtcNow,
            LogState = 'V'
        });
        return context.SaveChangesAsync();
    }

    public IAsyncEnumerable<RunLog> GetByDateAsync(UserAccount userAccount, DateTime forDate)
    {
        var fromDate = new DateTime(forDate.Year, forDate.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        var toDate = fromDate.AddMonths(2);
        return context.RunLog.Include(r => r.Route).Where(r => r.UserAccountId == userAccount.Id && r.Date >= fromDate && r.Date < toDate && r.LogState == 'V').AsAsyncEnumerable();
    }

    public IAsyncEnumerable<RunLog> GetLatestRunByRouteAsync(UserAccount userAccount, IEnumerable<Models.Route> routes)
    {
        var routeIds = routes.Select(r => r.Id).ToHashSet();
        return context.RunLog
            .Where(r => r.UserAccountId == userAccount.Id && routeIds.Contains(r.RouteId))
            .GroupBy(r => r.RouteId)
            .Select(g => g.OrderByDescending(rl => rl.Date).First())
            .AsAsyncEnumerable();
    }
}