using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public class PaceService(ILogger<PaceService> logger, IUserService userService)
    : IPaceService
{
    public string TimeTakenDisplayFormat(string? timeTakenValue)
    {
        var timeTaken = TimeTaken(timeTakenValue);
        return timeTaken == null ? (timeTakenValue ?? "") : timeTaken.Value.ToString(timeTaken.Value.TotalHours > 1 ? "hh\\:mm\\:ss" : "mm\\:ss");
    }

    public TimeSpan? TimeTaken(string? timeTaken)
    {
        if (string.IsNullOrEmpty(timeTaken))
            return null;
        var timePortions = timeTaken.Split(':');

        var hours = 0;
        int minutes;
        var seconds = 0;

        // mm or mm:ss or hh:mm:ss
        if (timePortions.Length == 1) // mm
        {
            _ = int.TryParse(timePortions[0], out minutes);
        }
        else if (timePortions.Length == 2) // mm:ss
        {
            _ = int.TryParse(timePortions[0], out minutes);
            _ = int.TryParse(timePortions[1], out seconds);
        }
        else // hh:mm:ss
        {
            _ = int.TryParse(timePortions[0], out hours);
            _ = int.TryParse(timePortions[1], out minutes);
            _ = int.TryParse(timePortions[2], out seconds);
        }

        TimeSpan time = new(hours, minutes, seconds);
        logger.LogDebug("Parsed time [{TimeTaken}] to [{Time}] ({Hours}:{Minutes}:{Seconds})", timeTaken, time, hours, minutes, seconds);
        return time.Ticks == 0 ? null : time;
    }

    public string CalculatePace(UserAccount userAccount, RunLog runLog)
        => CalculatePace(userAccount, TimeTaken(runLog.TimeTaken), runLog.Route.Distance, null) ?? "unknown pace";

    public string? CalculatePace(UserAccount userAccount, TimeSpan? timeTaken, decimal routeDistanceInMeters, string? defaultIfInvalid)
    {
        if (timeTaken == null || routeDistanceInMeters == 0)
            return defaultIfInvalid;

        var pace = Convert.ToDecimal(timeTaken.Value.TotalSeconds) / userService.ToUserDistanceUnits(routeDistanceInMeters, userAccount);
        logger.LogDebug("TimeTaken: {TimeTaken} ({TimeTakenSeconds}s); Distance: {Distance}; Pace: {Pace}", timeTaken, timeTaken.Value.TotalSeconds, routeDistanceInMeters, pace);
        return string.Format("{0}:{1} min/{2}", Convert.ToInt32(Math.Floor(pace / 60)).ToString("0"), Math.Floor(pace % 60).ToString("00"), (DistanceUnits)userAccount.DistanceUnits switch { DistanceUnits.Miles => "mile", DistanceUnits.Kilometers => "km", _ => "" });
    }

    public decimal ConvertFromMilesToKm(decimal fromMiles) => fromMiles * UserService.KilometersToMiles;
    public decimal ConvertFromKmToMiles(decimal fromKm) => fromKm / UserService.KilometersToMiles;
}