using System.Xml.Linq;

namespace liXMaiLips.Services;

public class AttributeExtractor
{
    // Attribute or leaf tag name -> sorted list of unique values
    public Dictionary<string, List<string>>? ExtractUniqueAttributes(string xmlContent)
    {
        var attributesMap = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(xmlContent))
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            XDocument doc = XDocument.Parse(xmlContent);
            if (doc.Root == null)
                return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in doc.Root.Descendants())
            {
                // 1. Extract XML attributes on the element (excluding xmlns declarations)
                foreach (var attr in element.Attributes())
                {
                    if (attr.IsNamespaceDeclaration)
                        continue;

                    AddUniqueValue(attributesMap, attr.Name.LocalName, attr.Value);
                }

                // 2. Extract leaf child elements that hold text values directly
                if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
                {
                    AddUniqueValue(attributesMap, element.Name.LocalName, element.Value);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Attribute parsing error: {ex.Message}");
            return null;
        }

        return attributesMap.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AddUniqueValue(
        Dictionary<string, HashSet<string>> map,
        string key,
        string rawValue)
    {
        string trimmed = rawValue.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        if (!map.TryGetValue(key, out var values))
        {
            values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            map[key] = values;
        }

        values.Add(trimmed);
    }
}