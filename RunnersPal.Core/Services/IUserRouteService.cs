using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public interface IUserRouteService
{
    Task<(List<Models.Route> Routes, Dictionary<int, RunLog> RunsByRouteId)> GetUserRoutesAsync(UserAccount userAccount, string? findFilter, string? sort);
}