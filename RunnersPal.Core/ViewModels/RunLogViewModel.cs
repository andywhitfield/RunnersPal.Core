using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels
{
    public class RunLogViewModel
    {
        public RunLogViewModel(HttpContext context, dynamic runLogEvent) : this(context, new[] { runLogEvent }) { }
        public RunLogViewModel(HttpContext context, IEnumerable<dynamic> runLogEvents)
        {
            RunLogModels = runLogEvents.Select(e => new RunLogModel(context, e));
            Routes = Enumerable.Empty<RoutePalViewModel.RouteModel>();
        }

        public IEnumerable<RunLogModel> RunLogModels { get; private set; }
        public object RunLogEventsToJson()
        {
            return RunLogModels.Select(m => m.RunLogEventToJson());
        }

        public IEnumerable<RoutePalViewModel.RouteModel> Routes { get; set; }

        public class RunLogModel
        {
            private readonly HttpContext context;
            public readonly dynamic RunLogEvent;

            public RunLogModel(HttpContext context, dynamic runLogEvent)
            {
                this.context = context;
                RunLogEvent = runLogEvent;

                TimeTaken = runLogEvent.TimeTaken;
                Route = ((object)RunLogEvent).Route();
                Distance = ((object)Route).Distance().ConvertTo(context.UserDistanceUnits());

                var paceData = new PaceData { Distance = Distance, Time = TimeTaken, Calc = "Pace" };
                var paceCalc = new PaceCalculator();
                paceCalc.Calculate(paceData);
                Pace = paceData;
            }

            public string TimeTaken { get; private set; }
            public dynamic Route { get; private set; }
            public Distance Distance { get; private set; }
            public PaceData Pace { get; private set; }

            public string Title
            {
                get
                {
                    var userUnits = context.UserDistanceUnits();
                    return Distance.BaseDistance.ToString("0.##") + " " + userUnits.UnitsToString("a") +
                        " in " + TimeTaken + "\n" + Pace.Pace + " min/" + userUnits.UnitsToString("a.s");
                }
            }

            public object RunLogEventToJson()
            {
                var systemRoute = Route.RouteType == RunnersPal.Core.Models.Route.SystemRoute.ToString();
                return new
                {
                    Completed = true,
                    id = RunLogEvent.Id,
                    title = Title,
                    start = RunLogEvent.Date.ToString("dd MMM yyyy"),
                    date = RunLogEvent.Date.ToString("dd MMM yyyy"),
                    distance = string.IsNullOrWhiteSpace(Route.MapPoints) && !systemRoute
                               ? Distance.BaseDistance.ToString("0.##")
                               : systemRoute ? Route.Name : (Route.Name + ", " + Distance.BaseDistance.ToString("0.##") + " " + Distance.BaseUnits.UnitsToString("a")),
                    pace = Pace.Pace,
                    time = TimeTaken,
                    route = string.IsNullOrWhiteSpace(Route.MapPoints) && !systemRoute ? -1 : Route.Id,
                    routeType = systemRoute ? "common" : "user",
                    comment = RunLogEvent.Comment
                };
            }
        }
    }
}