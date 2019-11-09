using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RunnersPal.Core.Extensions;

namespace RunnersPal.Core.ViewModels.Binders
{
    public class NewRunDataBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            DateTime? date = null;
            DateTime parsedDate;
            Trace.TraceInformation("Parsing date: {0}", bindingContext.GetString("date"));
            if (DateTime.TryParseExact(bindingContext.GetString("date"), "ddd, d MMM yyyy HH':'mm':'ss 'UTC'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate.ToUniversalTime();
            else if (DateTime.TryParseExact(bindingContext.GetString("date"), "ddd, d MMM yyyy HH':'mm':'ss 'GMT'", null, DateTimeStyles.AssumeUniversal, out parsedDate))
                date = parsedDate;

            var model = new NewRunData {
                RunLogId = bindingContext.GetLong("runLogId"),
                Date = date,
                Distance = bindingContext.GetDouble("distance"),
                Route = bindingContext.GetLong("route"),
                Time = bindingContext.GetString("time"),
                Comment = bindingContext.GetString("comment").MaxLength(1000)
            };
            if ((model.Route ?? 0) == -2)
                model.NewRoute = new RouteData {
                    Distance = bindingContext.GetDouble("distance") ?? 0.0,
                    Name = bindingContext.GetString("newRouteName"),
                    Notes = bindingContext.GetString("newRouteNotes"),
                    Points = bindingContext.GetString("newRoutePoints"),
                    Public = bindingContext.GetBool("newRoutePublic")
                };

            bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }
    }
}