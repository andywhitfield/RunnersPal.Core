using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers.ApiModels;

public record RouteListApiModel(Pagination Pagination, IEnumerable<RouteApiModel> Routes);
public record RouteApiModel(int Id, string Name, string Distance, string LastRunDate);
public record ShareApiModel(string ShareLink);