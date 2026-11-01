using CsvHelper;

namespace RunnersPal.Core.Pages.ViewModels;

[CsvRecord]
public partial class DownloadRunLogEventViewModel
{
    [CsvColumn(Index = 0)]
    public string Date { get; set; } = "";
    [CsvColumn(Index = 1)]
    public string Distance { get; set; } = "";
    [CsvColumn(Index = 2)]
    public string Time { get; set; } = "";
    [CsvColumn(Index = 3)]
    public string Pace { get; set; } = "";
    [CsvColumn(Index = 4)]
    public string RouteName { get; set; } = "";
    [CsvColumn(Index = 5)]
    public string Comment { get; set; } = "";
}