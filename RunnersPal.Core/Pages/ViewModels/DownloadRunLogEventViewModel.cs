using CsvHelper.Configuration.Attributes;

namespace RunnersPal.Core.Pages.ViewModels;

public class DownloadRunLogEventViewModel
{
    [Index(0)]
    public string Date { get; set; } = "";
    [Index(1)]
    public string Distance { get; set; } = "";
    [Index(2)]
    public string Time { get; set; } = "";
    [Index(3)]
    public string Pace { get; set; } = "";
    [Index(4)]
    public string RouteName { get; set; } = "";
    [Index(5)]
    public string Comment { get; set; } = "";
}