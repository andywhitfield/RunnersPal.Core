using System.ComponentModel.DataAnnotations;

namespace RunnersPal.Core.ViewModels
{
    public class RouteData
    {
        [Required]
        public long Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Notes { get; set; }
        public bool? Public { get; set; }
        [Required]
        public string Points { get; set; }
        [Required]
        public double Distance { get; set; }
    }
}