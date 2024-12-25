using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public interface IRouteRepository
{
    ValueTask<Models.Route?> GetRouteAsync(int routeId);
    Task<Models.Route> CreateNewRouteAsync(UserAccount user, string name, string points, decimal distance, string? notes);
    Task<Models.Route> UpdateRouteAsync(Models.Route route, UserAccount user, string name, string points, decimal disable, string? notes);
    Task<IEnumerable<Models.Route>> GetRoutesByUserAsync(UserAccount userAccount);
}