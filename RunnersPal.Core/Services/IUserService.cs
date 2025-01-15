using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public interface IUserService
{
    bool IsLoggedIn { get; }
    decimal ToDistanceUnits(decimal distanceInMeters, DistanceUnits distanceUnits);
    decimal ToUserDistanceUnits(decimal distanceInMeters, UserAccount userAccount);
    string ToUserDistance(decimal distanceInMeters, UserAccount userAccount);
    decimal ToDistanceInMeters(decimal distanceInUserUnits, UserAccount userAccount);
}