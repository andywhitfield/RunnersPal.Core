namespace RunnersPal.Core.Controllers.ApiModels;

public record ElevationApiModel(string Stats, IEnumerable<string> Series, IEnumerable<double> Elevation);
