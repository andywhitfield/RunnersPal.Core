using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Core.Models;

public class UserAccount
{
    public int Id { get; set; }
    [Required]
    public required string DisplayName { get; set; } = "";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityDate { get; set; } = DateTime.UtcNow;
    public string? EmailAddress { get; set; }
    [Required]
    public required string OriginalHostAddress { get; set; } = "";
    public char UserType { get; set; } = 'N';
    public int DistanceUnits { get; set; } = (int)Models.DistanceUnits.Miles;
}