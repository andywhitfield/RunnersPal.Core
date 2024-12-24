namespace RunnersPal.Core.Services;

public class Page(int pageNumber, bool isSelected = false, bool isNextPageSkipped = false)
{
    public int PageNumber { init; get; } = pageNumber;
    public bool IsSelected { init; get; } = isSelected;
    public bool IsNextPageSkipped { init; get; } = isNextPageSkipped;

    public override string ToString() => $"{PageNumber}{(IsSelected ? " (selected)" : "")}";
    public override int GetHashCode() => PageNumber;
    public override bool Equals(object? obj)
        => obj is Page other && PageNumber == other.PageNumber && IsSelected == other.IsSelected;
}