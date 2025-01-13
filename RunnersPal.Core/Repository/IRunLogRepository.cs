using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public interface IRunLogRepository
{
    Task<RunLog?> GetRunLogAsync(int id);
    Task<RunLog> CreateNewAsync(UserAccount userAccount, DateTime runDate, Models.Route route, string timeTaken, string? comment, RunLog? replacedRunLog);
    IAsyncEnumerable<RunLog> GetByDateAsync(UserAccount userAccount, DateTime forDate);
    IAsyncEnumerable<RunLog> GetLatestRunByRouteAsync(UserAccount userAccount, IEnumerable<Models.Route> routes);
    Task DeleteRunLogAsync(RunLog existingActivity);
}