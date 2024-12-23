using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class RouteRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context)
    : IRouteRepository
{
    public ValueTask<Models.Route?> GetRouteAsync(int routeId)
        => context.Route.FindAsync(routeId);

    public async Task<Models.Route> CreateNewRouteAsync(UserAccount user, string name, string points, string? notes)
    {
        logger.LogDebug("Creating new route [{Name}] for [{User}]", name, user.Id);
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = user,
            Name = name,
            MapPoints = points,
            Notes = notes,
            RouteType = Models.Route.PrivateRoute
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }
}