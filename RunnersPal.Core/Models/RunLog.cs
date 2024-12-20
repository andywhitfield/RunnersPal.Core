using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RunnersPal.Core.Models;

public class RunLog
{
    public int Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public int RouteId { get; set; }
    [Required]
    public required Route Route { get; set; }
    [Required]
    public required string TimeTaken { get; set; } = "";
    public int UserAccountId { get; set; }
    [Required]
    public required UserAccount UserAccount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
    public char LogState { get; set; }
    public int? ReplacesRunLogId { get; set; }
    public Route? ReplacesRunLog { get; set; }
}