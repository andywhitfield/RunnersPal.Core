using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public interface IPaceService
{
    TimeSpan? TimeTaken(string? timeTaken);
    string CalculatePace(RunLog runLog);
}