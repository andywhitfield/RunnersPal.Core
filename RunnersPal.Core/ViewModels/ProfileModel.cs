﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using RunnersPal.Core.Calculators;
using RunnersPal.Core.Extensions;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.ViewModels;

public class ProfileModel
{
    public static WeightData DefaultWeightData()
    {
        return new WeightData { Units = "kg", Kg = 0, Lbs = 0, St = 0, StLbs = 0 };
    }

    public ProfileModel() { }
    public ProfileModel(HttpContext context)
    {
        if (!context.HasValidUserAccount())
        {
            Name = "";
            Weight = new WeightData { Units = "kg" };
            return;
        }

        var userAccount = context.UserAccount();
        Name = userAccount.DisplayName;
        
        Weight = DefaultWeightData();
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

        DistUnits = (int)context.UserDistanceUnits();
    }

    public string? Message { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public WeightData Weight { get; set; }

    [Required]
    public int? DistUnits { get; set; }
}