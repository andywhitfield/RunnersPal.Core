using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Core.Models;

public class SiteSettings
{
    public int Id { get; set; }
    [Required]
    public required string Domain { get; set; } = "";
    [Required]
    public required string Identifier { get; set; } = "";
    public string? SettingValue { get; set; }
}