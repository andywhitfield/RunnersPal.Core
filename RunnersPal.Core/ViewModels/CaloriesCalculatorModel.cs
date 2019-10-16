using Microsoft.AspNetCore.Http;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels
{
    public class CaloriesCalculatorModel
    {
        public CaloriesCalculatorModel(HttpContext context)
        {
            Weight = ProfileModel.DefaultWeightData();
            if (!context.HasValidUserAccount())
                return;

            var userAccount = context.UserAccount();
            
            var userPref = ((object)userAccount).UserPrefs().Latest();
            if (userPref != null)
            {
                Weight.Units = userPref.WeightUnits;
                switch (Weight.Units)
                {
                    case "kg":
                        Weight.Kg = userPref.Weight;
                        break;
                    case "lbs":
                    case "st":
                        Weight.Lbs = userPref.Weight;
                        Weight.Units = "lbs";
                        break;
                }
                Weight.UpdateFromUnits();
                if (userPref.WeightUnits == "st")
                    Weight.Units = "st";
            }
        }

        public WeightData Weight { get; set; }
    }
}