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

            if (element.Attributes != null)
            {
                foreach (XmlAttribute attr in element.Attributes)
                {
                    if (!attr.Name.StartsWith("xmlns") && !string.IsNullOrWhiteSpace(attr.Value))
                        fields[attr.LocalName] = attr.Value.Trim();
                }
            }

            if (childElements.Count > 0)
            {
                foreach (var child in childElements)
                {
                    if (!string.IsNullOrWhiteSpace(child.InnerText))
                        fields[child.LocalName] = child.InnerText.Trim();
                }
            }
            else if ((element.Attributes == null || element.Attributes.Count == 0) &&
                     !string.IsNullOrWhiteSpace(element.InnerText))
            {
                fields[element.LocalName] = element.InnerText.Trim();
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
                    nextInherited[attr.LocalName] = attr.Value.Trim();
            }
        }

        foreach (var child in childElements)
        {
            TraverseNode(child, nextInherited, parameters, results);
        }
    }

    private static bool IsLeafRecord(XmlElement element, List<XmlElement> childElements)
    {
        bool hasAttrs = element.Attributes != null && element.Attributes.Count > 0;

        if (childElements.Count == 0)
            return hasAttrs || (!string.IsNullOrWhiteSpace(element.InnerText) && element.ParentNode?.ParentNode is XmlDocument);

        return childElements.All(c =>
            !c.ChildNodes.OfType<XmlElement>().Any() &&
            (c.Attributes == null || c.Attributes.Count == 0));
    }
}