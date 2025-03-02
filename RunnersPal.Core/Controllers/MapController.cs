using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;
using RunnersPal.Core.Geolib;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MapController(
    ILogger<MapController> logger,
    IUserService userService,
    IUserAccountRepository userAccountRepository,
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
            logger.LogWarning("Only one point, not calculating elevation");
            return BadRequest();
        }

        logger.LogDebug("Calculating elevation for latlng coords: [{LatLng}]", latLng.AsEnumerable());

        var elevation = (await elevationService.CalculateElevationAsync([.. latLng.Select(ll => (Coordinate)ll)]))?.ToList();
        if (elevation == null)
        {
            logger.LogWarning("Failed calculating elevation");
            return BadRequest();
        }

        var distanceUnit = userService.IsLoggedIn
            ? (DistanceUnits)(await userAccountRepository.GetUserAccountAsync(User)).DistanceUnits
            : (unit ?? "") switch { "km" => DistanceUnits.Kilometers, "miles" => DistanceUnits.Miles, _ => DistanceUnits.Kilometers };
        double? min = default;
        double? max = default;
        double total = 0;
        double? lastElevation = null;
        foreach (var e in elevation)
        {
            min = min == null ? e.Elevation : Math.Min(min.Value, e.Elevation);
            max = max == null ? e.Elevation : Math.Max(max.Value, e.Elevation);
            if (lastElevation != null)
                total += e.Elevation > lastElevation ? e.Elevation - lastElevation.Value : 0;
            lastElevation = e.Elevation;
        }

        return Ok(new ElevationApiModel(
            min != null && max != null ? $"Highest: {max.Value:0}m, Lowest: {min.Value:0}m, Total ascent: {total:0}m" : "",
            [.. elevation.Select(e => userService.ToDistanceUnits(Convert.ToDecimal(e.Distance), distanceUnit).ToString("0.0"))],
            [.. elevation.Select(i => i.Elevation)]));
    }
}
