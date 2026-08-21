using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Repository;
using RunnersPal.Core.Services;

namespace RunnersPal.Core.Pages.User;

public class IndexModel(
    ILogger<IndexModel> logger,
    IUserService userService,
    IUserAccountRepository userAccountRepository,
    IRouteRepository routeRepository,
    IRunLogRepository runLogRepository,
    IPaceService paceService)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ByPeriod { get; set; }
    [BindProperty(SupportsGet = true)] public int? RouteId { get; set; }

    public string? RouteName { get; private set; }
    public bool ShowGraph { get; set; }
    public string ChartType { get; set; } = "line";
    public string DateSeries { get; set; } = "[]";
    public string Distance { get; set; } = "[]";
    public string Pace { get; set; } = "[]";
    public string DistanceUnit { get; set; } = "";
    public string PaceUnit { get; set; } = "";

    public async Task<IActionResult> OnGet()
    {
        if (!userService.IsLoggedIn)
        {
            logger.LogInformation("User is not logged on, nothing to show");
            return Page();
        }

        var userAccount = await userAccountRepository.GetUserAccountAsync(User);
        DistanceUnit = userAccount.DistanceUnits == (int)Models.DistanceUnits.Miles ? "miles" : "km";
        PaceUnit = userAccount.DistanceUnits == (int)Models.DistanceUnits.Miles ? "min/miles" : "min/km";

        if (RouteId != null)
        {
            var route = await routeRepository.GetRouteAsync(RouteId.Value);
            if (route == null ||
                (route.RouteType != Models.Route.SystemRoute && string.IsNullOrEmpty(route.MapPoints)) ||
                (route.RouteType != Models.Route.SystemRoute && route.Creator != userAccount.Id))
            {
                logger.LogWarning("Route {RouteId} cannot be found, is a manual distance route, or is not owned by the current user {UserAccountId}", RouteId, userAccount.Id);
                return BadRequest();
            }

            var allActivities = await runLogRepository.GetAllLogRunsAsync(userAccount, route.Id).ToListAsync();
            if (allActivities.Count == 0)
            {
                logger.LogInformation("No run activities for route {RouteId}, nothing to show", route.Id);
                return Page();
            }

            ShowGraph = true;
            ByPeriod = route.RouteType == Models.Route.SystemRoute ? "byroutesystem" : "byroute";
            var allDatesAndDistances = allActivities.Select(r => new { r.Date, r.Route.Distance, Pace = paceService.CalculatePaceAsTimeSpan(userAccount, r) ?? TimeSpan.Zero });

            if (allActivities.Count == 1)
                ChartType = "column";
            DateSeries = "[" + string.Join(',', allActivities.Select(a => $"'{a.Date:dd MMM yyyy}'")) + "]";
            Distance = "[" + string.Join(',', allActivities.Select(a => a.Route.Distance)) + "]";
            Pace = "[" + string.Join(',', allActivities.Select(a => decimal.Round(Convert.ToDecimal((paceService.CalculatePaceAsTimeSpan(userAccount, a) ?? TimeSpan.Zero).TotalSeconds / 60), 2))) + "]";
            RouteName = allActivities.First().Route.Name;

            return Page();
        }

        var mostRecentActivity = await runLogRepository.GetLatestRunLogAsync(userAccount);
        if (mostRecentActivity == null)
        {
            logger.LogInformation("No run activities, nothing to show");
            return Page();
        }

        ShowGraph = true;
        IAsyncEnumerable<Models.RunLog> qualifyingActivities;
        Func<DateTime, DateTime> periodGrouping;
        Func<DateTime, DateTime> nextPeriod;
        string periodDateFormat;

        if (string.Equals(ByPeriod, "bymonth", StringComparison.InvariantCultureIgnoreCase))
        {
            ByPeriod = "bymonth";
            qualifyingActivities = runLogRepository.GetRunLogByDateRangeAsync(userAccount, mostRecentActivity.Date.AddMonths(-18), mostRecentActivity.Date.Date.AddDays(1));
            periodGrouping = dt => new(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            nextPeriod = dt => dt.AddMonths(1);
            periodDateFormat = "MMM yyyy";
        }
        else if (string.Equals(ByPeriod, "byyear", StringComparison.InvariantCultureIgnoreCase))
        {
            ByPeriod = "byyear";
            qualifyingActivities = runLogRepository.GetRunLogByDateRangeAsync(userAccount, mostRecentActivity.Date.AddYears(-18), mostRecentActivity.Date.Date.AddDays(1));
            periodGrouping = dt => new(dt.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            nextPeriod = dt => dt.AddYears(1);
            periodDateFormat = "yyyy";
        }
        else
        {
            ByPeriod = "byweek";
            qualifyingActivities = runLogRepository.GetRunLogByDateRangeAsync(userAccount, mostRecentActivity.Date.AddMonths(-6), mostRecentActivity.Date.Date.AddDays(1));
            periodGrouping = WeekEnding;
            nextPeriod = dt => dt.AddDays(7);
            periodDateFormat = "dd MMM";
        }

        var datesAndDistances = qualifyingActivities.Select(r => new { r.Date, r.Route.Distance, Pace = paceService.CalculatePaceAsTimeSpan(userAccount, r) ?? TimeSpan.Zero });
        var aggregated = await datesAndDistances
            .GroupBy(d => periodGrouping(d.Date))
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Period = g.Key,
                Distance = decimal.Round(userService.ToUserDistanceUnits(g.Sum(x => x.Distance), userAccount), 2),
                Pace = decimal.Round(g.Average(x => Convert.ToDecimal(x.Pace.TotalSeconds / 60)), 2)
            })
            .ToListAsync();

        var fromDate = aggregated.Min(a => a.Period);
        var toDate = aggregated.Max(a => a.Period);
        var allAggregated =
            (from period in DateRange(fromDate, toDate, nextPeriod)
             join aggregate in aggregated on period equals aggregate.Period into allPeriodsAgg
             from g in allPeriodsAgg.DefaultIfEmpty()
             select new
             {
                 Period = period,
                 Distance = g?.Distance ?? 0,
                 Pace = g?.Pace ?? 0
             }).ToList();

        if (allAggregated.Count == 1)
            ChartType = "column";
        DateSeries = "[" + string.Join(',', allAggregated.Select(a => $"'{a.Period.ToString(periodDateFormat)}'")) + "]";
        Distance = "[" + string.Join(',', allAggregated.Select(a => a.Distance)) + "]";
        Pace = "[" + string.Join(',', allAggregated.Select(a => a.Pace)) + "]";

        return Page();
    }

    private static IEnumerable<DateTime> DateRange(DateTime fromDate, DateTime toDate, Func<DateTime, DateTime> incrementFunc)
    {
        DateTime dt = fromDate;
        do
        {
            yield return dt;
            dt = incrementFunc(dt);
        } while (dt <= toDate);
    }

    private static DateTime WeekEnding(DateTime dt)
    {
        var date = dt.Date;
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => date.AddDays(6),
            DayOfWeek.Tuesday => date.AddDays(5),
            DayOfWeek.Wednesday => date.AddDays(4),
            DayOfWeek.Thursday => date.AddDays(3),
            DayOfWeek.Friday => date.AddDays(2),
            DayOfWeek.Saturday => date.AddDays(1),
            _ => date
        };
    }
}
