namespace RunnersPal.Core.Controllers.ApiModels;

public record RunLogEventApiModel(int Id, DateOnly Date, string Title, string Pace);