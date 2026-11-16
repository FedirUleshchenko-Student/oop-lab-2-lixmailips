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

        // Leaf element with attributes or text (when parent is not already a flat record)
        if (!el.HasElements)
            return el.HasAttributes || (!string.IsNullOrWhiteSpace(el.Value) && el.Parent.Parent == null);

        // Element whose children are purely simple leaf fields
        return el.Elements().All(child => !child.HasElements && !child.HasAttributes);
    }

    private static XmlRecord BuildRecord(XElement el)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Inherit ancestor grouping attributes (e.g., Faculty -> Department -> Record)
        foreach (var ancestor in el.Ancestors().Reverse())
        {
            foreach (var attr in ancestor.Attributes())
            {
                if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                    fields[attr.Name.LocalName] = attr.Value.Trim();
            }
        }

        // Read attributes on the record element itself
        foreach (var attr in el.Attributes())
        {
            if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                fields[attr.Name.LocalName] = attr.Value.Trim();
        }

        // Read leaf child elements or direct text value
        if (el.HasElements)
        {
            foreach (var child in el.Elements())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                    fields[child.Name.LocalName] = child.Value.Trim();
            }
        }
        else if (!el.HasAttributes && !string.IsNullOrWhiteSpace(el.Value))
        {
            fields[el.Name.LocalName] = el.Value.Trim();
        }

        return new XmlRecord { Fields = fields };
    }
}