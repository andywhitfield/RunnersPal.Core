using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class RunLogRepository(ILogger<RunLogRepository> logger, SqliteDataContext context)
    : IRunLogRepository
{
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
}