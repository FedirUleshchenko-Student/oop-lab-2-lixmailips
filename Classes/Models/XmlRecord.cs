namespace liXMaiLips.Models;

public record XmlRecord
{
    public Dictionary<string, string> Fields = new(StringComparer.OrdinalIgnoreCase);

    // Operator []
    public string this[string key] => Fields.TryGetValue(key, out var val) ? val : string.Empty;

    public override string ToString()
    {
        return string.Join(" | ", Fields.Select(f => $"{f.Key}: {f.Value}"));
    }
}