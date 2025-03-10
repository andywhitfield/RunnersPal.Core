using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class RouteRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context)
    : IRouteRepository
{
    public ValueTask<Models.Route?> GetRouteAsync(int routeId)
        => context.Route.FindAsync(routeId);

    public async Task<Models.Route> CreateNewRouteAsync(UserAccount user, string name, string points, decimal distance, string? notes, int? replacesRouteId)
    {
        logger.LogDebug("Creating new route [{Name}] for [{User}]", name, user.Id);
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = user,
            Name = name,
            MapPoints = points,
            Distance = distance,
            DistanceUnits = (int)DistanceUnits.Meters,
            Notes = notes,
            RouteType = Models.Route.PrivateRoute,
            ReplacesRouteId = replacesRouteId
        });
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    public async Task<Models.Route> UpdateRouteAsync(Models.Route route, UserAccount user, string name, string points, decimal distance, string? notes)
    {
        logger.LogDebug("Updating route [{RouteId}] with new route [{Name}] for [{User}]", route.Id, name, user.Id);
        var newRoute = context.Route.Add(new()
        {
            CreatorAccount = user,
            Name = name,
            MapPoints = points,
            Distance = distance,
            DistanceUnits = (int)DistanceUnits.Kilometers,
            Notes = notes,
            RouteType = Models.Route.PrivateRoute,
            ReplacesRoute = route
        });
        route.RouteType = Models.Route.DeletedRoute;
        await context.SaveChangesAsync();
        return newRoute.Entity;
    }

    public Task DeleteRouteAsync(Models.Route route)
    {
        logger.LogDebug("Deleting route [{RouteId}]", route.Id);
        route.RouteType = Models.Route.DeletedRoute;
        return context.SaveChangesAsync();
    }

    public async Task<List<Models.Route>> GetRoutesByUserAsync(UserAccount userAccount, string? find)
        => await context.Route.Where(r =>
            r.Creator == userAccount.Id &&
            r.RouteType == Models.Route.PrivateRoute &&
            r.MapPoints != null &&
            r.MapPoints.Length > 0 &&
            (string.IsNullOrEmpty(find) || EF.Functions.Like(r.Name, $"%{find.Trim()}%") || (r.Notes != null && EF.Functions.Like(r.Notes, $"%{find.Trim()}%")))).ToListAsync();

    public IAsyncEnumerable<Models.Route> GetSystemRoutesAsync() => context.Route.Where(r => r.RouteType == Models.Route.SystemRoute).OrderBy(r => r.Id).AsAsyncEnumerable();
}