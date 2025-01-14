using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class CalculatorController(
    ILogger<CalculatorController> logger,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IUserService userService,
    IPaceService paceService)
    : ControllerBase
{
    private const int _rounding = 4;

    [HttpGet("distance")]
    public async Task<ActionResult<DistanceApiModel>> Distance([FromQuery] string? km, [FromQuery] string? mile, [FromQuery] string? source)
    {
        switch (source ?? "")
        {
            case "mile":
                if (!decimal.TryParse(mile, out var fromMiles))
                {
                    logger.LogInformation("From mile [{Mile}] cannot be parsed", mile);
                    return BadRequest();
                }
                return new DistanceApiModel(fromMiles, decimal.Round(paceService.ConvertFromMilesToKm(fromMiles), _rounding));
            case "km":
                if (!decimal.TryParse(km, out var fromKm))
                {
                    logger.LogInformation("From km [{Km}] cannot be parsed", km);
                    return BadRequest();
                }
                return new DistanceApiModel(decimal.Round(paceService.ConvertFromKmToMiles(fromKm), _rounding), fromKm);
            case "halfmarathon":
                var hmKm = (await routeRepository.GetSystemRoutesAsync().Where(r => r.Name == "Half-marathon").SingleAsync()).Distance / 1000;
                return new DistanceApiModel(decimal.Round(paceService.ConvertFromKmToMiles(hmKm), _rounding), hmKm);
            case "marathon":
                var fullKm = (await routeRepository.GetSystemRoutesAsync().Where(r => r.Name == "Marathon").SingleAsync()).Distance / 1000;
                return new DistanceApiModel(decimal.Round(paceService.ConvertFromKmToMiles(fullKm), _rounding), fullKm);
            default:
                return BadRequest();
        }
    }

    [Authorize]
    [HttpGet("pace")]
    public async Task<ActionResult<PaceApiModel>> Pace([FromQuery] string? timeTaken, [FromQuery] int? distanceType,
        decimal? distanceManual, int? routeId, decimal? mapDistance)
    {
        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        decimal? routeDistanceInMeters = null;
        string? distance = null;

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

                routeDistanceInMeters = userService.ToDistanceInMeters(distanceManual ?? 0, userAccount);
                break;
            case 3:
                if (routeId == null)
                {
                    logger.LogWarning("Getting pace for a user route, but no route id passed");
                    return BadRequest();
                }

                var userRoute = await routeRepository.GetRouteAsync(routeId.Value);
                if (userRoute == null || userRoute.RouteType != Models.Route.PrivateRoute || userRoute.Creator != userAccount.Id)
                {
                    logger.LogWarning("Getting pace for a user route, but route id passed is not a user route: {RouteId}", routeId);
                    return BadRequest();
                }

                routeDistanceInMeters = userRoute.Distance;
                distance = $"{userService.ToUserDistance(routeDistanceInMeters ?? 0, userAccount)} @ ";
                break;
            case 4:
                if ((mapDistance ?? 0) <= 0)
                {
                    logger.LogWarning("Getting pace for a newly mapped route, but no map distance was passed");
                    return BadRequest();
                }
                
                routeDistanceInMeters = mapDistance;
                distance = $"{userService.ToUserDistance(routeDistanceInMeters ?? 0, userAccount)} @ ";
                break;
        }

        if (routeDistanceInMeters == null)
            return BadRequest();

        var pace = paceService.CalculatePace(userAccount, paceService.TimeTaken(timeTaken), routeDistanceInMeters.Value, null);
        if (pace == null)
            return BadRequest();

        return Ok(new PaceApiModel((distance ?? "") + pace));
    }
}