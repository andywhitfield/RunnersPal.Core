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
    IUserService userService,
    IPaceService paceService)
    : ControllerBase
{
    [HttpGet("activities")]
    public async IAsyncEnumerable<RunLogEventApiModel> Activities([FromQuery] DateTime date)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        logger.LogDebug("Getting run log activities for user account id = {UserAccountId}", userAccount.Id);
        await foreach (var runLog in runLogRepository.GetByDateAsync(userAccount, date))
            yield return new(runLog.Id, DateOnly.FromDateTime(runLog.Date), $"{userService.ToUserDistance(runLog.Route.Distance, userAccount)} in {paceService.TimeTakenDisplayFormat(runLog.TimeTaken)}", paceService.CalculatePace(userAccount, runLog));
    }
}
