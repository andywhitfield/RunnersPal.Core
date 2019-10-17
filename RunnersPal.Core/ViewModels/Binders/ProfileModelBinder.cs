using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RunnersPal.Core.Calculators;

namespace RunnersPal.Core.ViewModels.Binders
{
    public class ProfileModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            bindingContext.Result = ModelBindingResult.Success(new ProfileModel
            {
                DistUnits = bindingContext.GetInt("distUnits"),
                Name = bindingContext.GetString("name"),
                Weight = new WeightData
                {
                    Kg = bindingContext.GetDouble("weightKg"),
                    Lbs = bindingContext.GetDouble("weightLbs"),
                    St = bindingContext.GetDouble("weightSt"),
                    StLbs = bindingContext.GetDouble("weightStLbs"),
                    Units = bindingContext.GetString("weightUnits"),
                }
            });

            return Task.CompletedTask;
        }
    }
}