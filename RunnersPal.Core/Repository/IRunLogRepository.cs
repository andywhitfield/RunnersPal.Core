using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public interface IRunLogRepository
{
    Task CreateNewAsync(UserAccount userAccount, DateTime runDate, Models.Route route, string timeTaken, string? comment);
}