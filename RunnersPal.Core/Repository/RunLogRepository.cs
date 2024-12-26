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
}