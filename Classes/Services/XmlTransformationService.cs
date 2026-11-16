using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using liXMaiLips.Models;

namespace liXMaiLips.Services;

public class XmlTransformationService
{
    /// <summary>
    /// Filters the source XML according to active search parameters while preserving
    /// the original XML structure, attributes, and nested elements of matching records.
    /// </summary>
    public string BuildFilteredXml(string xmlContent, DynamicSearchParams? parameters)
    {
        if (string.IsNullOrWhiteSpace(xmlContent) || parameters == null || parameters.IsEmpty())
            return xmlContent;

        XDocument doc = XDocument.Parse(xmlContent);
        if (doc.Root == null)
            return xmlContent;

        // Track non-record container elements before removal to prune empty groups afterward
        var groupingContainers = doc.Root
            .Descendants()
            .Where(el => el.HasElements && !IsLeafRecordElement(el))
            .Reverse()
            .ToList();

        var recordElements = doc.Root
            .Descendants()
            .Where(IsLeafRecordElement)
            .ToList();

        foreach (var el in recordElements)
        {
            var record = BuildRecord(el);
            if (record.Fields.Count == 0 || !XmlFilterMatcher.Matches(record, parameters))
            {
                el.Remove();
            }
        }

        // Remove grouping containers that became empty after filtering
        foreach (var container in groupingContainers)
        {
            if (container.Parent != null && !container.HasElements)
            {
                container.Remove();
            }
        }

        return doc.Declaration != null
            ? $"{doc.Declaration}{Environment.NewLine}{doc}"
            : doc.ToString();
    }

    /// <summary>
    /// Transforms XML content into HTML using the provided XSLT stylesheet.
    /// </summary>
    public string TransformToHtml(string xmlContent, string xslContent)
    {
        var xslt = new XslCompiledTransform();

        using (var xslStringReader = new StringReader(xslContent))
        using (var xslXmlReader = XmlReader.Create(xslStringReader))
        {
            xslt.Load(xslXmlReader);
        }

        var sb = new StringBuilder();
        var writerSettings = xslt.OutputSettings?.Clone() ?? new XmlWriterSettings
        {
            Indent = true,
            ConformanceLevel = ConformanceLevel.Auto
        };

        using (var xmlStringReader = new StringReader(xmlContent))
        using (var xmlReader = XmlReader.Create(xmlStringReader))
        using (var htmlWriter = XmlWriter.Create(sb, writerSettings))
        {
            xslt.Transform(xmlReader, htmlWriter);
        }

        return sb.ToString();
    }

    private static bool IsLeafRecordElement(XElement el)
    {
        if (el.Parent == null)
            return false;

        if (!el.HasElements)
            return el.Parent.Parent == null;

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

        // Leaf child elements (both child attributes like scientificWork.title and text values)
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