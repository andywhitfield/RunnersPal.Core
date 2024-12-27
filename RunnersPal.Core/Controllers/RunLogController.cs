using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class RunLogController(ILogger<RunLogController> logger,
    IUserAccountRepository userAccountRepository,
    IRunLogRepository runLogRepository)
    : ControllerBase
{
    [HttpGet("activities")]
    public async IAsyncEnumerable<RunLogEventApiModel> Activities([FromQuery] DateTime date)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting run log activities for {UserAccountId}", userAccount.Id);
        await foreach (var runLog in runLogRepository.GetByDateAsync(userAccount, date))
            yield return new(runLog.Id, DateOnly.FromDateTime(runLog.Date), $"{runLog.Route.Distance / 1000 /* TODO user unit conversion */} km in {runLog.TimeTaken}", "xx:xx min/km" /* TODO calculate pace */);
    }
}
