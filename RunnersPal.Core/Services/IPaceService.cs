using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public interface IPaceService
{
    TimeSpan? TimeTaken(string? timeTaken);
    string CalculatePace(UserAccount userAccount, RunLog runLog);
    string? CalculatePace(UserAccount userAccount, TimeSpan? timeTaken, decimal routeDistanceInMeters, string? defaultIfInvalid);
}