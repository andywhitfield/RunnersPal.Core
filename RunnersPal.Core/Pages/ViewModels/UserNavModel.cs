namespace RunnersPal.Core.Pages.ViewModels;

public record UserNavModel(HttpRequest Request)
{
    private const string _selectedCssClass = "rp-selected";

    public string Css(string prop, string propVal = "") =>
        prop switch
        {
            "log" when Request.Path.StartsWithSegments(new PathString("/runlog")) => _selectedCssClass,
            "routes" when Request.Path.StartsWithSegments(new PathString("/routepal")) => _selectedCssClass,
            "stats" when Request.Path.StartsWithSegments(new PathString("/user")) => _selectedCssClass,
            "calcs" when Request.Path.StartsWithSegments(new PathString("/calculators")) => _selectedCssClass,
            _ => ""
        };
}