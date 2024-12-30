namespace RunnersPal.Core.Controllers.ApiModels;

public record RouteListApiModel(int Page, int PageCount, IEnumerable<RouteApiModel> Routes);
public record RouteApiModel(int Id, string Name, string Distance);