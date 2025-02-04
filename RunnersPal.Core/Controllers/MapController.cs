using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Controllers.ApiModels;

namespace RunnersPal.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MapController(ILogger<MapController> logger)
    : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpPost("elevation")]
    public ActionResult<ElevationApiModel> Elevation([FromForm] string? points)
    {
        var latLng = JsonSerializer.Deserialize<LatLngApiModel[]>(points ?? "", _jsonOptions);
        if (latLng == null)
        {
            logger.LogWarning("Could not parse points, returning BadRequest: {Points}", points);
            return BadRequest();
        }

        logger.LogDebug("Calculating elevation for latlng: [{LatLng}]", latLng.AsEnumerable());
        return Ok(new ElevationApiModel(["1M", "2M", "3M"], [100, 110, 105.5m]));
    }
}