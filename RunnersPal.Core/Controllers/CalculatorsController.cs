using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Data;
using RunnersPal.Core.Data.Caching;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;
using RunnersPal.Core.ViewModels;

namespace RunnersPal.Core.Controllers
{
    public class CalculatorsController : Controller
    {
        private PaceCalculator paceCalc = new PaceCalculator();
        private DistanceCalculator distanceCalc = new DistanceCalculator();
        private WeightCalculator weightCalc = new WeightCalculator();
        private CaloriesCalculator caloriesCalc = new CaloriesCalculator();
        private readonly IDataCache dataCache;

        public CalculatorsController(IDataCache dataCache)
        {
            this.dataCache = dataCache;
        }
        
        public ActionResult Index() { return View(); }
        public ActionResult Pace() { return View(); }
        public ActionResult Distance() { return View(); }
        public ActionResult Calories() { return View(new CaloriesCalculatorModel(HttpContext)); }

        [HttpPost]
        public ActionResult CalcPace(PaceData paceCalculation)
        {
            if (!ModelState.IsValid)
                return Json(paceCalculation);

            Trace.TraceInformation("Calculating pace (route/dist/time/pace/calc): {0}/{1}/{2}/{3}/{4}", paceCalculation.Route, paceCalculation.Distance, paceCalculation.Time, paceCalculation.Pace, paceCalculation.Calc);
            if (paceCalculation.HasRoute)
            {
                var userUnits = paceCalculation.Distance.BaseUnits;
                var route = MassiveDB.Current.FindRoute(paceCalculation.Route.Value);
                if (route != null)
                    paceCalculation.Distance = new Distance((double)route.Distance, (DistanceUnits)route.DistanceUnits).ConvertTo(userUnits);
            }

            paceCalc.Calculate(paceCalculation);
            Trace.TraceInformation("Calculated pace (route/dist/time/pace/calc): {0}/{1}/{2}/{3}/{4}", paceCalculation.Route, paceCalculation.Distance, paceCalculation.Time, paceCalculation.Pace, paceCalculation.Calc);

            return Json(paceCalculation);
        }

        [HttpPost]
        public ActionResult CalcDistance(DistanceData distanceCalculation)
        {
            if (!ModelState.IsValid)
                return Json(distanceCalculation);

            distanceCalc.Calculate(distanceCalculation);

            return Json(distanceCalculation);
        }

        [HttpPost]
        public ActionResult CalcWeight(WeightData weightCalculation)
        {
            if (!ModelState.IsValid)
                return Json(new { Result = false, Calc = weightCalculation });

            weightCalc.Calculate(weightCalculation);

            return Json(new { Result = true, Calc = weightCalculation });
        }

        [HttpPost]
        public ActionResult CalcCalories(WeightData weightData, Distance distanceData)
        {
            if (!ModelState.IsValid)
                return Json(new { Result = false });

            return Json(new { Result = true, Calories = caloriesCalc.Calculate(distanceData, weightData) });
        }

        [HttpPost]
        public ActionResult AutoCalcCalories(string date, int? route, double? distance)
        {
            WeightData weight = null;

            if (HttpContext.HasValidUserAccount(dataCache) && date != null)
            {
                var userAccount = HttpContext.UserAccount(dataCache);

                DateTime onDate;
                if (!DateTime.TryParseExact(date, "ddd, d MMM yyyy HH':'mm':'ss 'UTC'", null, DateTimeStyles.AssumeUniversal, out onDate))
                    onDate = DateTime.UtcNow;

                weight = ProfileModel.DefaultWeightData();
                var userPref = ((object)userAccount).UserPrefs().Latest(onDate);
                if (userPref != null)
                {
                    weight.Units = userPref.WeightUnits;
                    switch (weight.Units)
                    {
                        case "kg":
                            weight.Kg = userPref.Weight;
                            break;
                        case "lbs":
                        case "st":
                            weight.Lbs = userPref.Weight;
                            weight.Units = "lbs";
                            break;
                    }
                    weight.UpdateFromUnits();
                    if (userPref.WeightUnits == "st")
                        weight.Units = "st";
                }
            }

            if (weight == null || weight.UnitWeight == 0 || (!route.HasValue && !distance.HasValue))
                return Json(new { Result = false });

            Distance actualDistance = null;

            if (route.HasValue && route.Value > 0)
            {
                var dbRoute = MassiveDB.Current.FindRoute(route.Value);
                if (dbRoute != null)
                    actualDistance = new Distance((double)dbRoute.Distance, (DistanceUnits)dbRoute.DistanceUnits).ConvertTo(HttpContext.UserDistanceUnits(dataCache));
            }

            if (distance.HasValue && distance.Value > 0)
                actualDistance = new Distance(distance.Value, HttpContext.UserDistanceUnits(dataCache));

            if (actualDistance == null)
                return Json(new { Result = false });

            return Json(new { Result = true, Calories = caloriesCalc.Calculate(actualDistance, weight) });
        }
    }
}
