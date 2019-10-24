using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels.Binders
{
    public class CustomModelsBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Metadata.ModelType == typeof(Distance))
                return new BinderTypeModelBinder(typeof(DistanceBinder));
            if (context.Metadata.ModelType == typeof(PaceData))
                return new BinderTypeModelBinder(typeof(PaceDataBinder));
            if (context.Metadata.ModelType == typeof(NewRunData))
                return new BinderTypeModelBinder(typeof(NewRunDataBinder));
            if (context.Metadata.ModelType == typeof(ProfileModel))
                return new BinderTypeModelBinder(typeof(ProfileModelBinder));

            return null;
        }
    }
}