using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Data;
using RunnersPal.Core.Data.Caching;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;
using RunnersPal.Core.ViewModels;

namespace RunnersPal.Core.Controllers
{
    public class RoutePalController : Controller
    {
        private readonly ILogger<RoutePalController> logger;
        private readonly IDataCache dataCache;

        public RoutePalController(ILogger<RoutePalController> logger, IDataCache dataCache)
        {
            this.logger = logger;
            this.dataCache = dataCache;
        }
        
        public ActionResult Index()
        {
            if (Request.Query["route"].FirstOrDefault() == "0" && HttpContext.HasValidUserAccount())
            {
                try
                {
                    var routeData = HttpContext.Session.Get<RouteData>("rp_RouteInfoPreLogin");
                    if (routeData != null)
                    {
                        var saveInfo = Save(routeData) as JsonResult;
                        if (saveInfo != null)
                        {
                            dynamic json = saveInfo.Value;
                            if (json.Completed)
                            {
                                return Redirect(Url.Action("index", "routepal", new { route = json.Route.Id }));
                            }
                        }
                    }
                }
                finally
                {
                    HttpContext.Session.Remove("rp_RouteInfoPreLogin");
                }
            }

            return View();
        }

        [HttpPost]
        public ActionResult MyRoutes()
        {
            return Json(new { Completed = true, Routes = RoutePalViewModel.RoutesForCurrentUser(HttpContext, dataCache) });
        }

        [HttpPost]
        public ActionResult Load(long? id)
        {
            if (!id.HasValue)
                return Json(new { Completed = false, Reason = "No route was provided." });
            if (id == 0)
            {
                var routeData = HttpContext.Session.Get<RouteData>("rp_RouteInfoPreLogin");
                if (routeData != null)
                    return Json(new { Completed = true, Route = new { Id = routeData.Id, Name = routeData.Name, Notes = routeData.Notes ?? "", Public = routeData.Public ?? false, Points = routeData.Points ?? "[]", Distance = routeData.Distance, PublicOther = false, Deleted = false } });
                else
                    return Json(new { Completed = false, Reason = "No route was provided." });
            }

            var route = MassiveDB.Current.FindRoute(id.Value, true);
            if (route == null)
                return Json(new { Completed = false, Reason = "Cannot find the specified route." });
            if (route.RouteType == Route.SystemRoute.ToString())
                return Json(new { Completed = false, Reason = "Cannot find your route." });

            var currentUser = HttpContext.UserAccount();
            var isRouteOwnedByAnotherUser = currentUser == null || currentUser.Id != route.Creator;
            if (route.RouteType == Route.PrivateRoute.ToString() && isRouteOwnedByAnotherUser)
                return Json(new { Completed = false, Reason = "The route you are trying to load was either not created by you or is not public. Please check you are logged in and try again." });

            return Json(new { Completed = true, Route = new { Id = route.Id, Name = route.Name, Notes = route.Notes ?? "", Public = route.RouteType == Route.PublicRoute.ToString(), Points = string.IsNullOrWhiteSpace(route.MapPoints) ? "[]" : route.MapPoints, Distance = route.Distance, PublicOther = route.RouteType == Route.PublicRoute.ToString() && isRouteOwnedByAnotherUser, Deleted = route.RouteType == Route.DeletedRoute.ToString() } });
        }

        [HttpPost]
        public ActionResult Save(RouteData routeData)
        {
            if (!ModelState.IsValid)
                return Json(new { Completed = false, Reason = "Please provide a route name." });
            if (!HttpContext.HasValidUserAccount(dataCache))
                return Json(new { Completed = false, Reason = "Please create an account." });

            var userUnits = HttpContext.UserDistanceUnits(dataCache);
            Distance distance = new Distance(routeData.Distance, userUnits);

            logger.LogInformation("Saving route {0} name {1}, notes {2}, is public? {3}, points: {4}", routeData.Id, routeData.Name, routeData.Notes, routeData.Public, routeData.Points);

            string lastRun;
            string lastRunBy;
            if (routeData.Id == 0)
            {
                var newRoute = MassiveDB.Current.CreateRoute(HttpContext.UserAccount(dataCache), routeData.Name, routeData.Notes ?? "", distance, (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute, routeData.Points);
                routeData.Id = Convert.ToInt64(newRoute.Id);
                lastRun = "";
                lastRunBy = "";
            }
            else
            {
                var currentRoute = MassiveDB.Current.FindRoute(routeData.Id);
                var currentUser = HttpContext.UserAccount(dataCache);
                var isRouteOwnedByAnotherUser = currentUser.Id != currentRoute.Creator;

                if (isRouteOwnedByAnotherUser && currentRoute.RouteType != Route.PublicRoute.ToString())
                    return Json(new { Completed = false, Reason = "Cannot save the route - you can only save routes you have created." });

                if (isRouteOwnedByAnotherUser || currentRoute.MapPoints != routeData.Points)
                {
                    if (!isRouteOwnedByAnotherUser)
                    {
                        // delete old
                        currentRoute.RouteType = Route.DeletedRoute;
                        MassiveDB.Current.UpdateRoute(currentRoute);
                    }

                    // add new
                    currentRoute = MassiveDB.Current.CreateRoute(currentUser, routeData.Name, routeData.Notes ?? "", distance, (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute, routeData.Points, currentRoute.Id);
                    routeData.Id = Convert.ToInt64(currentRoute.Id);

                    lastRun = "";
                    lastRunBy = "";
                }
                else
                {
                    currentRoute.Name = routeData.Name;
                    currentRoute.Notes = routeData.Notes ?? "";
                    currentRoute.RouteType = (routeData.Public ?? false) ? Route.PublicRoute : Route.PrivateRoute;

                    MassiveDB.Current.UpdateRoute(currentRoute);

                    // get last run info
                    var runInfo = MassiveDB.Current.FindLatestRunLogForRoutes(new[] { routeData.Id }).FirstOrDefault();
                    if (runInfo == null)
                    {
                        lastRunBy = "";
                        lastRun = "";
                    }
                    else
                    {
                        lastRunBy = runInfo.DisplayName;
                        var runDate = ((object)runInfo.Date).ToDateTime();
                        lastRun = runDate == null ? "" : runDate.Value.ToString("ddd, dd/MMM/yyyy");
                    }
                }
            }

            return Json(new { Completed = true, Route = new { Id = routeData.Id, Name = routeData.Name, Notes = routeData.Notes ?? "", Public = routeData.Public ?? false, Points = routeData.Points, Distance = distance.BaseDistance, LastRun = lastRun, PublicOther = false, Deleted = false } });
        }

        [HttpPost]
        public ActionResult Delete(long? id)
        {
            if (!id.HasValue || id < 1)
                return Json(new { Completed = false, Reason = "No route was provided." });

            var currentUser = HttpContext.UserAccount();
            if (currentUser == null)
                return Json(new { Completed = false, Reason = "You must be logged in to delete the route. Please confirm you are logged in and try again." });

            var route = MassiveDB.Current.FindRoute(id.Value);
            if (route == null)
                return Json(new { Completed = false, Reason = "Cannot find the specified route." });
            if (route.RouteType != Route.PublicRoute.ToString() && route.RouteType != Route.PrivateRoute.ToString())
                return Json(new { Completed = false, Reason = "Cannot find your route." });

            if (currentUser.Id != route.Creator || currentUser.UserType == "A")
                return Json(new { Completed = false, Reason = "The route you are trying to delete was not created by you. Please check you are logged in correctly and try again." });

            route.RouteType = Route.DeletedRoute;
            MassiveDB.Current.UpdateRoute(route);
            return Json(new { Completed = true });
        }

        [HttpPost]
        public ActionResult Find(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Json(new { Completed = true, Routes = new object[0] });

            dynamic currentUser = HttpContext.HasValidUserAccount(dataCache) ? HttpContext.UserAccount(dataCache) : null;

            IEnumerable<dynamic> routes = MassiveDB.Current.SearchForRoutes(currentUser, q);
            IEnumerable<dynamic> runInfos = MassiveDB.Current.FindLatestRunLogForRoutes(routes.Select(r => (long)r.Id));

            var routeModels = routes.Select(route => new RoutePalViewModel.RouteModel(HttpContext, route)).ToList();
            foreach (var route in routeModels)
            {
                var runInfo = runInfos.FirstOrDefault(r => r.RouteId == route.Id);
                if (runInfo == null) continue;
                route.LastRunBy = runInfo.DisplayName;
                route.LastRunDate = ((object)runInfo.Date).ToDateTime();
            }

            return Json(new { Completed = true, Routes = routeModels.OrderByDescending(r => r.LastRunDate ?? r.CreatedDate) });
        }

        [HttpPost]
        public ActionResult BeforeLogin(RouteData routeData)
        {
            if (!ModelState.IsValid)
                return Json(new { Completed = false, Reason = "Please provide a route name." });

            logger.LogInformation("Saving route before login process - id {0}, name {1}, notes {2}, is public? {3}, points: {4}", routeData.Id, routeData.Name, routeData.Notes, routeData.Public, routeData.Points);
            HttpContext.Session.Set("rp_RouteInfoPreLogin", routeData);

            return Json(new { Completed = true });
        }
    }
}
