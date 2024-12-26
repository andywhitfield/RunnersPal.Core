namespace RunnersPal.Core.Services;

public static class DateTimeExtensions
{
    public static DateTime ParseDateTime(this string? date) =>
        DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var d) ? d : DateTime.Today;
}