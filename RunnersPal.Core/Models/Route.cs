using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunnersPal.Core.Models;

public class Route
{
    public int Id { get; set; }
    [Required]
    public required string Name { get; set; } = "";
    public string? Notes { get; set; }
    public decimal Distance { get; set; }
    public int DistanceUnits { get; set; }
    [ForeignKey(nameof(CreatorAccount))]
    public int Creator { get; set; }
    [Required]
    public required UserAccount CreatorAccount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public char RouteType { get; set; }
    public string? MapPoints { get; set; }
    public int? ReplacesRouteId { get; set; }
    public Route? ReplacesRoute { get; set; }
}