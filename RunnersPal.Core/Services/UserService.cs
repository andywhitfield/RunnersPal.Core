using System.Security.Claims;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    public const decimal KilometersToMiles = 1.609344m;

    public bool IsLoggedIn => !string.IsNullOrEmpty(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name));

    public decimal ToDistanceUnits(decimal distanceInMeters, DistanceUnits distanceUnits)
        =>  distanceInMeters / distanceUnits switch
        {
            DistanceUnits.Kilometers => 1000m,
            DistanceUnits.Miles => 1000m * KilometersToMiles,
            _ => throw new InvalidOperationException("Unknown distance unit: " + distanceUnits)
        };

    public decimal ToUserDistanceUnits(decimal distanceInMeters, UserAccount userAccount)
        => ToDistanceUnits(distanceInMeters, (DistanceUnits)userAccount.DistanceUnits);

    public string ToUserDistance(decimal distanceInMeters, UserAccount userAccount)
    {
        var distance = ToUserDistanceUnits(distanceInMeters, userAccount).ToString("0.#");
        return distance + ((DistanceUnits)userAccount.DistanceUnits switch
        {
            DistanceUnits.Kilometers => "km",
            DistanceUnits.Miles => "mile" + (distance == "1" ? "" : "s"),
            _ => throw new InvalidOperationException("Unknown distance unit for account: " + userAccount.Id + ": " + userAccount.DistanceUnits)
        });
    }

    public decimal ToDistanceInMeters(decimal distanceInUserUnits, UserAccount userAccount) 
        => distanceInUserUnits * (DistanceUnits)userAccount.DistanceUnits switch
        {
            DistanceUnits.Kilometers => 1000m,
            DistanceUnits.Miles => 1000m * KilometersToMiles,
            _ => throw new InvalidOperationException("Unknown distance unit for account: " + userAccount.Id + ": " + userAccount.DistanceUnits)
        };
}