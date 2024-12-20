using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Core.Models;

public class UserPref
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    [Required]
    public required UserAccount UserAccount { get; set; }
    public DateTime? ValidTo { get; set; }
    public double? Weight { get; set; }
    public string? WeightUnits { get; set; }
}