namespace liXMaiLips.Models;

public record DynamicSearchParams
{
    public Dictionary<string, List<string>> SelectedFilters = new();

    public bool IsEmpty() => !SelectedFilters.Any(f => f.Value != null && f.Value.Any());
}