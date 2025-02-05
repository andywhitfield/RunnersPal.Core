using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Geolib;
using RunnersPal.Core.Models;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MapController(
    ILogger<MapController> logger,
    IUserService userService,
    IElevationService elevationService)
    : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpPost("elevation")]
    public async Task<ActionResult<ElevationApiModel>> Elevation([FromForm] string? points, [FromForm] string? unit)
    {
        var latLng = JsonSerializer.Deserialize<LatLngApiModel[]>(points ?? "", _jsonOptions);
        if (latLng == null)
        {
            logger.LogWarning("Could not parse points, returning BadRequest: {Points}", points);
            return BadRequest();
        }

        if (latLng.Length < 2)
        {
            logger.LogInformation("Only one point, not calculating elevation");
            return Ok(new ElevationApiModel([], []));
        }

        logger.LogDebug("Calculating elevation for latlng coords: [{LatLng}]", latLng.AsEnumerable());

        var elevation = (await elevationService.CalculateElevationAsync([.. latLng.Select(ll => (Coordinate)ll)]))?.ToList();
        if (elevation == null)
            return BadRequest();

        return Ok(new ElevationApiModel([.. elevation.Select(e => ToUserUnit(e.Distance, unit).ToString("0.0"))], [.. elevation.Select(i => i.Elevation)]));
    }

    private decimal ToUserUnit(double distanceInMeters, string? unit)
        => userService.ToDistanceUnits(Convert.ToDecimal(distanceInMeters), (unit ?? "") switch { "km" => DistanceUnits.Kilometers, "miles" => DistanceUnits.Miles, _ => DistanceUnits.Kilometers });
}
