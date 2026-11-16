using liXMaiLips.Models;
using liXMaiLips.Services;
using System.Xml;

namespace liXMaiLips.Strategies;

public class DomParseStrategy : IXmlParseStrategy
{
    public List<XmlRecord> Search(string xmlContent, DynamicSearchParams parameters)
    {
        var results = new List<XmlRecord>();
        if (string.IsNullOrWhiteSpace(xmlContent))
            return results;

        var doc = new XmlDocument();
        doc.LoadXml(xmlContent);

        if (doc.DocumentElement == null)
            return results;

        TraverseNode(doc.DocumentElement, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), parameters, results);
        return results;
    }

    private static void TraverseNode(
        XmlElement element,
        Dictionary<string, string> inheritedAttributes,
        DynamicSearchParams parameters,
        List<XmlRecord> results)
    {
        bool isRoot = element.ParentNode is XmlDocument;
        var childElements = element.ChildNodes.OfType<XmlElement>().ToList();

        if (!isRoot && IsLeafRecord(element, childElements))
        {
            var fields = new Dictionary<string, string>(inheritedAttributes, StringComparer.OrdinalIgnoreCase);

            // Read attributes on the record element itself
            if (element.Attributes != null)
            {
                foreach (XmlAttribute attr in element.Attributes)
                {
                    if (!attr.Name.StartsWith("xmlns") && !string.IsNullOrWhiteSpace(attr.Value))
                        AppendFieldValue(fields, attr.LocalName, attr.Value);
                }
            }

            // Read leaf child elements (both child attributes and text values)
            if (childElements.Count > 0)
            {
                foreach (var child in childElements)
                {
                    if (child.Attributes != null)
                    {
                        foreach (XmlAttribute attr in child.Attributes)
                        {
                            if (!attr.Name.StartsWith("xmlns") && !string.IsNullOrWhiteSpace(attr.Value))
                                AppendFieldValue(fields, $"{child.LocalName}.{attr.LocalName}", attr.Value);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(child.InnerText))
                        AppendFieldValue(fields, child.LocalName, child.InnerText);
                }
            }
            else if ((element.Attributes == null || element.Attributes.Count == 0) &&
                     !string.IsNullOrWhiteSpace(element.InnerText))
            {
                AppendFieldValue(fields, element.LocalName, element.InnerText);
            }

            var record = new XmlRecord { Fields = fields };
            if (record.Fields.Count > 0 && XmlFilterMatcher.Matches(record, parameters))
                results.Add(record);

            return;
        }

        // Accumulate attributes from grouping containers
        var nextInherited = new Dictionary<string, string>(inheritedAttributes, StringComparer.OrdinalIgnoreCase);
        if (!isRoot && element.Attributes != null)
        {
            foreach (XmlAttribute attr in element.Attributes)
            {
                if (!attr.Name.StartsWith("xmlns") && !string.IsNullOrWhiteSpace(attr.Value))
                    AppendFieldValue(nextInherited, attr.LocalName, attr.Value);
            }
        }

        foreach (var child in childElements)
        {
            TraverseNode(child, nextInherited, parameters, results);
        }
    }

    private static bool IsLeafRecord(XmlElement element, List<XmlElement> childElements)
    {
        if (childElements.Count == 0)
            return element.ParentNode?.ParentNode is XmlDocument;

        return childElements.All(c => !c.ChildNodes.OfType<XmlElement>().Any());
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