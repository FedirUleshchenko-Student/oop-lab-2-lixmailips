using liXMaiLips.Models;
using liXMaiLips.Services;
using System.Xml.Linq;

namespace liXMaiLips.Strategies;

public class LinqToXmlParseStrategy : IXmlParseStrategy
{
    public List<XmlRecord> Search(string xmlContent, DynamicSearchParams parameters)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            return new List<XmlRecord>();

        XDocument doc = XDocument.Parse(xmlContent);
        if (doc.Root == null)
            return new List<XmlRecord>();

        return doc.Root
            .Descendants()
            .Where(IsLeafRecordElement)
            .Select(BuildRecord)
            .Where(record => record.Fields.Count > 0 && XmlFilterMatcher.Matches(record, parameters))
            .ToList();
    }

    private static bool IsLeafRecordElement(XElement el)
    {
        if (el.Parent == null)
            return false;

        // Standalone leaf element only if its parent is not already a record container
        if (!el.HasElements)
            return el.Parent.Parent == null;

        // Record container whose children are all leaf elements (with text and/or attributes)
        return el.Elements().All(child => !child.HasElements);
    }

    private static XmlRecord BuildRecord(XElement el)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Inherit ancestor grouping attributes
        foreach (var ancestor in el.Ancestors().Reverse())
        {
            foreach (var attr in ancestor.Attributes())
            {
                if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                    AppendFieldValue(fields, attr.Name.LocalName, attr.Value);
            }
        }

        // Attributes on the record element itself
        foreach (var attr in el.Attributes())
        {
            if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                AppendFieldValue(fields, attr.Name.LocalName, attr.Value);
        }

        // Leaf child elements (both text values and child attributes like scientificWork.title)
        if (el.HasElements)
        {
            foreach (var child in el.Elements())
            {
                foreach (var attr in child.Attributes())
                {
                    if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                        AppendFieldValue(fields, $"{child.Name.LocalName}.{attr.Name.LocalName}", attr.Value);
                }

                if (!string.IsNullOrWhiteSpace(child.Value))
                    AppendFieldValue(fields, child.Name.LocalName, child.Value);
            }
        }
        else if (!el.HasAttributes && !string.IsNullOrWhiteSpace(el.Value))
        {
            AppendFieldValue(fields, el.Name.LocalName, el.Value);
        }

        return new XmlRecord { Fields = fields };
    }

    private static void AppendFieldValue(Dictionary<string, string> fields, string key, string rawValue)
    {
        string trimmed = rawValue.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        if (fields.TryGetValue(key, out var existing))
            fields[key] = $"{existing}; {trimmed}";
        else
            fields[key] = trimmed;
    }
}