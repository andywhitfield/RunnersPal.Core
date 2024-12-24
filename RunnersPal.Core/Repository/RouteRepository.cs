using Microsoft.EntityFrameworkCore;
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

    // TODO order by last run, then by id
    public async Task<IEnumerable<Models.Route>> GetRoutesByUserAsync(UserAccount userAccount)
        => await context.Route.Where(r => r.Creator == userAccount.Id && r.RouteType == Models.Route.PrivateRoute).OrderByDescending(r => r.Id).ToListAsync();
}