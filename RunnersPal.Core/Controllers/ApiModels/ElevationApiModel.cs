namespace RunnersPal.Core.Controllers.ApiModels;

public record ElevationApiModel(IEnumerable<string> Series, IEnumerable<double> Elevation);
