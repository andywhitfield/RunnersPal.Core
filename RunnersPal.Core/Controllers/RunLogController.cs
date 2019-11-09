using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;
using RunnersPal.Core.ViewModels;

namespace RunnersPal.Core.Controllers
{
    public class RunLogController : Controller
    {
        public ActionResult Index()
        {
            return View(new RunLogViewModel(HttpContext, Enumerable.Empty<dynamic>()));
        }

        public ActionResult AllEvents(string start, string end)
        {
            DateTime.TryParseExact(start, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal, out var startDate);
            DateTime.TryParseExact(end, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal, out var endDate);
            var model = new RunLogViewModel(HttpContext, MassiveDB.Current.FindRunLogEvents(HttpContext.UserAccount(), false, startDate, endDate));
            return Json(model.RunLogEventsToJson());
        }

        [HttpPost]
        public ActionResult Add(NewRunData newRunData)
        {
            return AddRunLogEvent(newRunData).Item1;
        }

        [HttpPost]
        public ActionResult View(int runLogId)
        {
            if (!HttpContext.HasValidUserAccount())
                return Json(new { Completed = false, Reason = "Please log in / create an account." });

            Trace.TraceInformation("Loading run log id {0}", runLogId);
            var runLogEvent = MassiveDB.Current.FindRunLogEvent(runLogId);
            if (runLogEvent == null)
                return Json(new { Completed = false, Reason = "Cannot find this event - please refresh and try again." });
            if (runLogEvent.UserAccountId != HttpContext.UserAccount().Id)
                return Json(new { Completed = false, Reason = "You are not allowed to view this event - please refresh and try again." });
            if (runLogEvent.LogState == "D")
                return Json(new { Completed = false, Reason = "This event has been deleted - please refresh and try again." });

            var model = new RunLogViewModel(HttpContext, runLogEvent);
            return Json(model.RunLogModels.Single().RunLogEventToJson());
        }

        [HttpPost]
        public ActionResult Delete(long runLogId)
        {
            if (!HttpContext.HasValidUserAccount())
                return Json(new { Completed = false, Reason = "Please log in / create an account." });

            Trace.TraceInformation("Deleting run log id {0}", runLogId);
            var runLogEvent = MassiveDB.Current.FindRunLogEvent(runLogId);
            if (runLogEvent == null)
                return Json(new { Completed = false, Reason = "Cannot find event - please refresh and try again." });
            if (runLogEvent.LogState == "D")
                return Json(new { Completed = false, Reason = "This event has already been deleted - please refresh and try again." });
            if (runLogEvent.UserAccountId != HttpContext.UserAccount().Id)
                return Json(new { Completed = false, Reason = "You are not allowed to delete this event - please refresh and try again." });

            runLogEvent.LogState = "D";
            MassiveDB.Current.UpdateRunLogEvent(runLogEvent);

            var model = new RunLogViewModel(HttpContext, runLogEvent);
            return Json(model.RunLogModels.Single().RunLogEventToJson());
        }

        [HttpPost]
        public ActionResult Edit(NewRunData newRunData)
        {
            if (!ModelState.IsValid || (!newRunData.Distance.HasValue && !newRunData.Route.HasValue) || !newRunData.RunLogId.HasValue)
                return Json(new { Completed = false, Reason = "Please provide a valid route/distance and time." });
            if (!HttpContext.HasValidUserAccount())
                return Json(new { Completed = false, Reason = "Please create an account." });

            Trace.TraceInformation("Editing run event {0} for date {1}, route {2}, distance {3}, time {4}", newRunData.RunLogId, newRunData.Date, newRunData.Route, newRunData.Distance, newRunData.Time);

            var deleted = Delete(newRunData.RunLogId.Value) as JsonResult;
            var deletedJson = deleted.Value as dynamic;
            if (!deletedJson.Completed)
                return Json(new { Completed = false, Reason = "You are not allowed to edit this event - please refresh and try again." });

            var addedItem = AddRunLogEvent(newRunData);
            if (addedItem.Item2 == null)
                return addedItem.Item1;

            dynamic newRunLogEvent = addedItem.Item2;
            newRunLogEvent.ReplacesRunLogId = newRunData.RunLogId;
            MassiveDB.Current.UpdateRunLogEvent(newRunLogEvent);
            return addedItem.Item1;
        }

        private Tuple<JsonResult, object> AddRunLogEvent(NewRunData newRunData)
        {
            if (!ModelState.IsValid || newRunData.Route.GetValueOrDefault() == 0 || (!newRunData.Distance.HasValue && !newRunData.Route.HasValue))
                return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please provide a valid route/distance and time." }), (object)null);
            if (!HttpContext.HasValidUserAccount())
                return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please create an account." }), (object)null);

            Trace.TraceInformation("Creating run event for date {0}, route {1}, distance {2}, time {3}", newRunData.Date, newRunData.Route, newRunData.Distance, newRunData.Time);

            var userUnits = HttpContext.UserDistanceUnits();
            dynamic route = null;
            Distance distance = new Distance(newRunData.Distance ?? 0, userUnits);
            if (newRunData.Route.HasValue)
            {
                var routeId = newRunData.Route.Value;
                if (routeId > 0)
                {
                    route = MassiveDB.Current.FindRoute(routeId);
                    if (route == null)
                        return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Cannot find selected route, please choose another route and try again." }), (object)null);

                    distance = new Distance((double)route.Distance, (DistanceUnits)route.DistanceUnits);
                }
                else if (routeId == -1)
                {
                    // manual distance...distance shoud be > 0
                    if (distance.BaseDistance <= 0)
                        return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please enter a distance for your run and try again." }), (object)null);
                }
                else if (routeId == -2)
                {
                    // new mapped route
                    if (newRunData.NewRoute == null)
                        return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please map a route, add a name and then add a run log event." }), (object)null);
                    if (string.IsNullOrWhiteSpace(newRunData.NewRoute.Name))
                        return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please provide a route name." }), (object)null);
                    if (string.IsNullOrWhiteSpace(newRunData.NewRoute.Points) || newRunData.NewRoute.Points == "[]")
                        return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please add some points to the new route by double-clicking the map." }), (object)null);
                    route = MassiveDB.Current.CreateRoute(HttpContext.UserAccount(), newRunData.NewRoute.Name, newRunData.NewRoute.Notes ?? "", distance, (newRunData.NewRoute.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute, newRunData.NewRoute.Points);
                }
                else
                    return Tuple.Create(new JsonResult(new { Completed = false, Reason = "Please select or create a route for your run and try again." }), (object)null);
            }

            var runLogEvent = MassiveDB.Current.CreateRunLogEvent(HttpContext.UserAccount(), newRunData.Date.Value, distance, route, newRunData.NormalizedTime, newRunData.Comment);
            var model = new RunLogViewModel(HttpContext, runLogEvent);

            return Tuple.Create(new JsonResult(model.RunLogModels.Single().RunLogEventToJson()), (object)runLogEvent);
        }
    }
}
