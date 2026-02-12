using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Services;

public class UserRouteService(IRouteRepository routeRepository, IRunLogRepository runLogRepository)
    : IUserRouteService
{
    public async Task<(List<Models.Route> Routes, Dictionary<int, RunLog> RunsByRouteId)> GetUserRoutesAsync(UserAccount userAccount, string? findFilter, string? sort)
    {
        var userRoutes = await routeRepository.GetRoutesByUserAsync(userAccount, findFilter);
        var lastRunsForRoutes = await runLogRepository.GetLatestRunByRouteAsync(userAccount, userRoutes).ToDictionaryAsync(rl => rl.RouteId);
        if (sort == "distance")
            userRoutes.Sort((r1, r2) => r1.Distance.CompareTo(r2.Distance));
        else
            userRoutes.Sort((r1, r2) => -1 * (LastRun(r1) ?? DateOnly.FromDateTime(r1.CreatedDate)).CompareTo(LastRun(r2) ?? DateOnly.FromDateTime(r2.CreatedDate)));
        return (userRoutes, lastRunsForRoutes);

        DateOnly? LastRun(Models.Route route)
            => lastRunsForRoutes.TryGetValue(route.Id, out var runLog) ? DateOnly.FromDateTime(runLog.Date) : null;
    }
}