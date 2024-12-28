using RunnersPal.Core.Models;

namespace RunnersPal.Core.Services;

public class PaceService(ILogger<PaceService> logger)
    : IPaceService
{
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

    public string CalculatePace(RunLog runLog)
    {
        var timeTaken = TimeTaken(runLog.TimeTaken);
        if (timeTaken == null || runLog.Route.Distance == 0)
            return "unknown pace";

        var pace = Convert.ToDecimal(timeTaken.Value.TotalSeconds) / (runLog.Route.Distance / 1000 /* convert to miles if that's the user's units */);
        logger.LogDebug("TimeTaken: {TimeTaken} ({TimeTakenSeconds}s); Distance: {Distance}; Pace: {Pace}", timeTaken, timeTaken.Value.TotalSeconds, runLog.Route.Distance, pace);
        return string.Format("{0}:{1} min/km", Convert.ToInt32(Math.Floor(pace / 60)).ToString("0"), Math.Floor(pace % 60).ToString("00"));
    }
}