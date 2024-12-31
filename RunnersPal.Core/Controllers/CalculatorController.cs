using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces(MediaTypeNames.Application.Json)]
public class CalculatorController(
    ILogger<CalculatorController> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IPaceService paceService)
    : ControllerBase
{
    [HttpGet("pace")]
    public async Task<IActionResult> Pace([FromQuery] string? timeTaken, [FromQuery] int? distanceType,
        decimal? distanceManual, int? routeId, decimal? mapDistance)
    {
        decimal? routeDistanceInMeters = null;

        switch (distanceType)
        {
            case 1:
                if (routeId == null)
                {
                    logger.LogWarning("Getting pace for a system route, but no route id passed");
                    return BadRequest();
                }

                var systemRoute = await routeRepository.GetRouteAsync(routeId.Value);
                if (systemRoute == null || systemRoute.RouteType != Models.Route.SystemRoute)
                {
                    logger.LogWarning("Getting pace for a system route, but route id passed is not a system route: {RouteId}", routeId);
                    return BadRequest();
                }

                routeDistanceInMeters = systemRoute.Distance;
                break;
            case 2:
                if ((distanceManual ?? 0) <= 0)
                {
                    logger.LogWarning("Getting pace for a manual distance, but no manual distance was passed");
                    return BadRequest();
                }

                routeDistanceInMeters = distanceManual;
                break;
            case 3:
                if (routeId == null)
                {
                    logger.LogWarning("Getting pace for a user route, but no route id passed");
                    return BadRequest();
                }

                var userAccount = await userAccountRepository.GetUserAccountAsync(User);
                var userRoute = await routeRepository.GetRouteAsync(routeId.Value);
                if (userRoute == null || userRoute.RouteType != Models.Route.PrivateRoute || userRoute.Creator != userAccount.Id)
                {
                    logger.LogWarning("Getting pace for a user route, but route id passed is not a user route: {RouteId}", routeId);
                    return BadRequest();
                }

                routeDistanceInMeters = userRoute.Distance;
                break;
            case 4:
                if ((mapDistance ?? 0) <= 0)
                {
                    logger.LogWarning("Getting pace for a newly mapped route, but no map distance was passed");
                    return BadRequest();
                }
                
                routeDistanceInMeters = mapDistance;
                break;
        }

        if (routeDistanceInMeters == null)
            return BadRequest();

        var pace = paceService.CalculatePace(paceService.TimeTaken(timeTaken), routeDistanceInMeters.Value, null);
        if (pace == null)
            return BadRequest();

        return Ok(new PaceApiModel(pace));
    }
}