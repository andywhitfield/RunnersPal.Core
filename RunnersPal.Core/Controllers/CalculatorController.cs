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

    [HttpGet("pace/convert")]
    public ActionResult<PaceAllApiModel> PaceConvert([FromQuery] string? km, [FromQuery] string? mile, [FromQuery] string? source)
    {
        var pace = (source ?? "") switch
        {
            "mile" => PaceConvertFromMile(mile),
            "km" => PaceConvertFromKm(km),
            _ => null,
        };

        if (pace == null)
            return BadRequest();

        return pace;
    }

    [HttpGet("pace/all")]
    public ActionResult<PaceAllApiModel> PaceAll([FromQuery] string? distance, [FromQuery] string? timeTaken, [FromQuery] string? pace, [FromQuery] string? dest)
    {
        var paceAll = (dest ?? "") switch
        {
            "distance" => CalculateDistance(timeTaken, pace),
            "timetaken" => CalculateTimeTaken(distance, pace),
            "pace" => CalculatePace(distance, timeTaken),
            _ => null,
        };
        if (paceAll == null)
            return BadRequest();
        
        return paceAll;
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

    private PaceAllApiModel? CalculateDistance(string? timeTaken, string? pace)
    {
        var time = paceService.TimeTaken(timeTaken);
        if (time == null)
        {
            logger.LogInformation("Cannot parse time taken, returning bad request");
            return null;
        }

        var paceInKm = paceService.TimeTaken(pace);
        if (paceInKm == null)
        {
            logger.LogInformation("Cannot parse pace, returning bad request");
            return null;
        }

        var distance3 = Convert.ToDecimal(time.Value.TotalSeconds / paceInKm.Value.TotalSeconds);
        return new PaceAllApiModel(
            decimal.Round(distance3, _rounding),
            decimal.Round(paceService.ConvertFromKmToMiles(distance3), _rounding),
            "", "", "");    
    }

    private PaceAllApiModel? CalculateTimeTaken(string? distance, string? pace)
    {
        if (!decimal.TryParse(distance, out var distanceInKm))
        {
            logger.LogInformation("Cannot parse distance, returning bad request");
            return null;
        }

        var paceInKm = paceService.TimeTaken(pace);
        if (paceInKm == null)
        {
            logger.LogInformation("Cannot parse pace, returning bad request");
            return null;
        }

        var time = TimeSpan.FromSeconds(paceInKm.Value.TotalSeconds * Convert.ToDouble(distanceInKm));
        return new PaceAllApiModel(0, 0, paceService.TimeTakenDisplayFormat(time), "", "");
    }

    private PaceAllApiModel? CalculatePace(string? distance, [FromQuery] string? timeTaken)
    {
        if (!decimal.TryParse(distance, out var distanceInMeters))
        {
            logger.LogInformation("Cannot parse distance, returning bad request");
            return null;
        }

        distanceInMeters *= 1000;

        var time = paceService.TimeTaken(timeTaken);
        if (time == null)
        {
            logger.LogInformation("Cannot parse time taken, returning bad request");
            return null;
        }

        logger.LogDebug("Calculating pace given time {Time} and distance {DistanceInMeters}", time, distanceInMeters);

        var paceInKm = paceService.CalculatePace(Models.DistanceUnits.Kilometers, time, distanceInMeters, null, false);
        if (paceInKm == null)
            return null;
        var paceInMile = paceService.CalculatePace(Models.DistanceUnits.Miles, time, distanceInMeters, null, false);
        if (paceInMile == null)
            return null;

        return new PaceAllApiModel(0, 0, "", paceInKm, paceInMile);        
    }

    private PaceAllApiModel? PaceConvertFromMile(string? mile)
    {
        var paceInMileTime = paceService.TimeTaken(mile);
        if (paceInMileTime == null)
        {
            logger.LogInformation("From mile [{Mile}] cannot be parsed", mile);
            return null;
        }

        var paceInKm = paceService.CalculatePace(Models.DistanceUnits.Kilometers, paceInMileTime, paceService.ConvertFromMilesToKm(1000), null, false);
        if (paceInKm == null)
            return null;

        logger.LogDebug("Calculated km pace as {Pace}", paceInKm);

        return new PaceAllApiModel(0, 0, "", paceInKm, "");
    }

    private PaceAllApiModel? PaceConvertFromKm(string? km)
    {
        var paceInKmTime = paceService.TimeTaken(km);
        if (paceInKmTime == null)
        {
            logger.LogInformation("From km [{Km}] cannot be parsed", km);
            return null;
        }

        var paceInMile = paceService.CalculatePace(Models.DistanceUnits.Miles, paceInKmTime, 1000, null, false);
        if (paceInMile == null)
            return null;
        
        logger.LogDebug("Calculated mile pace as {Pace}", paceInMile);

        return new PaceAllApiModel(0, 0, "", "", paceInMile);
    }
}