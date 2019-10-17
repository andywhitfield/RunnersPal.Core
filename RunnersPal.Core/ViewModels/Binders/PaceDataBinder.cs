using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels.Binders
{
    public class PaceDataBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(new PaceData
            {
                Calc = bindingContext.GetString("calc"),
                Pace = bindingContext.GetString("pace"),
                Distance = new Distance(bindingContext.GetDouble("distance") ?? 0, bindingContext.HttpContext.UserDistanceUnits()),
                Time = bindingContext.GetString("time"),
                Route = bindingContext.GetLong("route")
            });

            return Task.CompletedTask;
        }
    }
}