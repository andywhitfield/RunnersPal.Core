using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class RunLogController(ILogger<RunLogController> logger,
    IUserAccountRepository userAccountRepository,
    IRunLogRepository runLogRepository,
    IPaceService paceService)
    : ControllerBase
{
    [HttpGet("activities")]
    public async IAsyncEnumerable<RunLogEventApiModel> Activities([FromQuery] DateTime date)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting run log activities for user account id = {UserAccountId}", userAccount.Id);
        await foreach (var runLog in runLogRepository.GetByDateAsync(userAccount, date))
            yield return new(runLog.Id, DateOnly.FromDateTime(runLog.Date), $"{(runLog.Route.Distance / 1000 /* TODO user unit conversion */).ToString("0.#")} km in {TimeTaken(runLog)}", paceService.CalculatePace(runLog));
    }

    private string TimeTaken(RunLog runLog)
    {
        var timeTaken = paceService.TimeTaken(runLog.TimeTaken);
        return timeTaken == null ? runLog.TimeTaken : timeTaken.Value.ToString(timeTaken.Value.TotalHours > 1 ? "hh\\:mm\\:ss" : "mm\\:ss");
    }
}
