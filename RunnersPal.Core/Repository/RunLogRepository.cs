using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class RunLogRepository(ILogger<RunLogRepository> logger, SqliteDataContext context)
    : IRunLogRepository
{
    public Task<RunLog?> GetRunLogAsync(int id) => context.RunLog.Include(r => r.Route).SingleOrDefaultAsync(r => r.Id == id && r.LogState == RunLog.LogStateValid);

    public async Task<RunLog> CreateNewAsync(UserAccount userAccount, DateTime runDate, Models.Route route, string timeTaken, string? comment, RunLog? replacedRunLog)
    {
        logger.LogDebug("Creating new run log for [{User}]", userAccount.Id);
        var newRunLog = context.RunLog.Add(new()
        {
            UserAccount = userAccount,
            Route = route,
            Date = runDate,
            TimeTaken = timeTaken,
            Comment = comment,
            CreatedDate = DateTime.UtcNow,
            LogState = RunLog.LogStateValid,
            ReplacesRunLogId = replacedRunLog?.Id
        });
        await context.SaveChangesAsync();
        return newRunLog.Entity;
    }

    public IAsyncEnumerable<RunLog> GetByDateAsync(UserAccount userAccount, DateTime forDate)
    {
        var fromDate = new DateTime(forDate.Year, forDate.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        var toDate = fromDate.AddMonths(3);
        return context.RunLog.Include(r => r.Route).Where(r => r.UserAccountId == userAccount.Id && r.Date >= fromDate && r.Date < toDate && r.LogState == RunLog.LogStateValid).AsAsyncEnumerable();
    }

    public IAsyncEnumerable<RunLog> GetLatestRunByRouteAsync(UserAccount userAccount, IEnumerable<Models.Route> routes)
    {
        var routeIds = routes.Select(r => r.Id).ToHashSet();
        return context.RunLog
            .Where(r => r.UserAccountId == userAccount.Id && routeIds.Contains(r.RouteId) && r.LogState == RunLog.LogStateValid)
            .GroupBy(r => r.RouteId)
            .Select(g => g.OrderByDescending(rl => rl.Date).First())
            .AsAsyncEnumerable();
    }

    public Task<RunLog?> GetLatestRunLogAsync(UserAccount userAccount)
        => context.RunLog.Include(r => r.Route).OrderByDescending(r => r.Date).FirstOrDefaultAsync(r => r.UserAccountId == userAccount.Id && r.LogState == RunLog.LogStateValid);

    public IAsyncEnumerable<RunLog> GetRunLogByDateRangeAsync(UserAccount userAccount, DateTime from, DateTime to)
        => context.RunLog
            .Include(r => r.Route)
            .Where(r => r.UserAccountId == userAccount.Id && r.Date >= from && r.Date <= to && r.LogState == RunLog.LogStateValid)
            .AsAsyncEnumerable();

    public Task DeleteRunLogAsync(RunLog existingActivity)
    {
        existingActivity.LogState = RunLog.LogStateDeleted;
        return context.SaveChangesAsync();
    }
}