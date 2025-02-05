using System.Text;
using System.Text.Json;
using RunnersPal.Core.Geolib;

namespace RunnersPal.Core.Services;

public class OpenElevationClient(
    ILogger<OpenElevationClient> logger,
    IHttpClientFactory httpClientFactory)
    : IOpenElevationClient
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<OpenElevationResponseModel?> LookupAsync(IEnumerable<Coordinate> coords)
    {
        using var client = httpClientFactory.CreateClient(nameof(OpenElevationClient));
        var jsonBody = JsonSerializer.Serialize(new { locations = coords.Select(c => new { latitude = c.Latitude, longitude = c.Longitude }).ToList() });
        logger.LogDebug("Sending elevation request: {JsonBody}", jsonBody);
        var elevationResponse = await client.PostAsync("/api/v1/lookup", new StringContent(jsonBody, Encoding.UTF8, "application/json"));
        if (!elevationResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Could not get elevation: {ResponseStatusCode}", elevationResponse.StatusCode);
            return null;
        }

        return await elevationResponse.Content.ReadFromJsonAsync<OpenElevationResponseModel>(_jsonOptions);
    }
}