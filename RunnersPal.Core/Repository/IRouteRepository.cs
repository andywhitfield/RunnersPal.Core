using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public interface IRouteRepository
{
    Task<Models.Route> CreateNewRouteAsync(UserAccount user, string name, string points, string? notes);
}