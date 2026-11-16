
namespace liXMaiLips.Models;

public class DynamicSearchParams
{
    public Dictionary<string, HashSet<string>> SelectedFilters = new(StringComparer.OrdinalIgnoreCase);

    public DynamicSearchParams() { }

    public DynamicSearchParams(string key, params string[] values)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var val in values)
        {
            if (!string.IsNullOrWhiteSpace(val))
                set.Add(val.Trim());
        }
        SelectedFilters[key] = set;
    }

    // Operator []
    public HashSet<string>? this[string key] =>
        SelectedFilters.TryGetValue(key, out var val) ? val : null;

    // TODO: Check real answer
    // Last:
    //public bool IsEmpty() => !SelectedFilters.Any(f => f.Value != null && f.Value.Any());
    public bool IsEmpty() => SelectedFilters.Count == 0;

    public void Clear() => SelectedFilters.Clear();

    public void Add(DynamicSearchParams other)
    {
        foreach(var (key, values) in other.SelectedFilters)
        {
            if (!SelectedFilters.TryGetValue(key, out var existingSet))
                SelectedFilters.Add(key, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            if (values == null) continue;

            foreach(var v in values)
            {
                SelectedFilters[key].Add(v);
            }
        };
    }

    public void Remove(DynamicSearchParams other)
    {
        foreach (var (key, values) in other.SelectedFilters)
        {
            if (!SelectedFilters.TryGetValue(key, out var existingSet))
                continue;

            // Remove the entire attribute filter if no specific values are passed
            if (values == null || values.Count == 0)
            {
                SelectedFilters.Remove(key);
                continue;
            }

            foreach (var v in values)
            {
                SelectedFilters[key].Remove(v);
            }
        }
    }

    public override string ToString()
    {
        if (IsEmpty())
            return "No filters";

        var formatted = SelectedFilters.Select(kv =>
            kv.Value.Count > 0
                ? $"{kv.Key}=[{string.Join(", ", kv.Value)}]"
                : $"{kv.Key}=[Any]");

        return string.Join("; ", formatted);
    }
}