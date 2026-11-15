
namespace liXMaiLips.Models;

public record DynamicSearchParams
{
    public Dictionary<string, HashSet<string>> SelectedFilters = new(StringComparer.OrdinalIgnoreCase);

    // Operator []
    public HashSet<string>? this[string key] => SelectedFilters.TryGetValue(key, out var val) ? val : null;

    // TODO: Check real answer
    public bool IsEmpty() => !SelectedFilters.Any(f => f.Value != null && f.Value.Any());

    public void Clear() => SelectedFilters.Clear();

    public void Add(DynamicSearchParams other)
    {
        foreach(var p in other.SelectedFilters)
        {
            foreach(var s in p.Value)
            {
                SelectedFilters[p.Key].Add(s);
            }
        };
    }

    public void Remove(DynamicSearchParams other)
    {
        foreach (var p in other.SelectedFilters)
        {
            foreach (var s in p.Value)
            {
                SelectedFilters[p.Key].Remove(s);
            }
        }
        ;
    }
}