namespace RunnersPal.Core.ViewModels.Binders
{
    /*
    public class ProfileModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            OnModelUpdating(controllerContext, bindingContext);
            bindingContext.ModelMetadata.Model = new ProfileModel
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
            };
            OnModelUpdated(controllerContext, bindingContext);
            return bindingContext.Model;
        }
    }
    */
}