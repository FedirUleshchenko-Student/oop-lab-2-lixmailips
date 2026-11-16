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
                bool isLeaf = !element.HasElements;
                bool isRecordContainer = element.HasElements && element.Elements().All(c => !c.HasElements);

                // Extract XML attributes (flatten leaf child attributes as "tagName.attrName")
                foreach (var attr in element.Attributes())
                {
                    if (attr.IsNamespaceDeclaration)
                        continue;

                    string key = isLeaf && element.Parent != null && element.Parent != doc.Root
                        ? $"{element.Name.LocalName}.{attr.Name.LocalName}"
                        : attr.Name.LocalName;

                    AddUniqueValue(attributesMap, key, attr.Value);
                }

                // Extract text value of leaf elements (e.g., fullName, faculty, scientificWork)
                if (isLeaf && !isRecordContainer && !string.IsNullOrWhiteSpace(element.Value))
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