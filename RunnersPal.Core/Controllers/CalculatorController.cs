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
    IRouteRepository routeRepository,
    IPaceService paceService)
    : ControllerBase
{
    [HttpGet("pace")]
    public async Task<IActionResult> Pace([FromQuery] string? timeTaken, [FromQuery] int? distanceType, decimal? distanceManual, int? routeId)
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
        }

        if (routeDistanceInMeters == null)
            return BadRequest();

        var pace = paceService.CalculatePace(paceService.TimeTaken(timeTaken), routeDistanceInMeters.Value, null);
        if (pace == null)
            return BadRequest();

        return Ok(new PaceApiModel(pace));
    }
}