using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels.Binders
{
    public class DistanceBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(new Distance(bindingContext.GetDouble("distance") ?? 0, bindingContext.HttpContext.UserDistanceUnits()));
            return Task.CompletedTask;
        }
    }
}