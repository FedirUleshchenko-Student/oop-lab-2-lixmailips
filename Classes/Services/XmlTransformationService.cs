using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using liXMaiLips.Models;
using liXMaiLips.Strategies;

namespace liXMaiLips.Services;

public class XmlTransformationService
{
    private sealed class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    /// <summary>
    /// Filters the source XML document according to active search parameters while preserving element hierarchy.
    /// </summary>
    public string BuildFilteredXml(string xmlContent, DynamicSearchParams? parameters)
    {
        if (string.IsNullOrWhiteSpace(xmlContent) || parameters == null || parameters.IsEmpty())
            return xmlContent;

        XDocument doc = XDocument.Parse(xmlContent);
        if (doc.Root == null)
            return xmlContent;

        var recordElements = doc.Root
            .Descendants()
            .Where(IsLeafRecordElement)
            .ToList();

        var toRemove = new List<XElement>();
        foreach (var el in recordElements)
        {
            var record = BuildRecord(el);
            if (record.Fields.Count == 0 || !XmlFilterMatcher.Matches(record, parameters))
                toRemove.Add(el);
        }

        foreach (var el in toRemove)
        {
            el.Remove();
        }

        // Remove empty grouping containers that no longer hold any records
        bool removedEmpty;
        do
        {
            removedEmpty = false;
            var emptyContainers = doc.Root
                .Descendants()
                .Where(e => !e.HasElements && !e.HasAttributes && string.IsNullOrWhiteSpace(e.Value))
                .ToList();

            foreach (var container in emptyContainers)
            {
                container.Remove();
                removedEmpty = true;
            }
        } while (removedEmpty);

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

        using var xslStringReader = new StringReader(xslContent);
        using var xslXmlReader = XmlReader.Create(xslStringReader);
        xslt.Load(xslXmlReader, XsltSettings.Default, new XmlUrlResolver());

        using var xmlStringReader = new StringReader(xmlContent);
        using var xmlReader = XmlReader.Create(xmlStringReader);

        using var htmlWriter = new Utf8StringWriter();
        using var xmlOutputWriter = XmlWriter.Create(htmlWriter, xslt.OutputSettings);

        xslt.Transform(xmlReader, xmlOutputWriter);
        return htmlWriter.ToString();
    }

    private static bool IsLeafRecordElement(XElement el)
    {
        if (el.Parent == null)
            return false;

        if (!el.HasElements)
            return el.HasAttributes || (!string.IsNullOrWhiteSpace(el.Value) && el.Parent.Parent == null);

        return el.Elements().All(child => !child.HasElements && !child.HasAttributes);
    }

    private static XmlRecord BuildRecord(XElement el)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ancestor in el.Ancestors().Reverse())
        {
            foreach (var attr in ancestor.Attributes())
            {
                if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                    fields[attr.Name.LocalName] = attr.Value.Trim();
            }
        }

        foreach (var attr in el.Attributes())
        {
            if (!attr.IsNamespaceDeclaration && !string.IsNullOrWhiteSpace(attr.Value))
                fields[attr.Name.LocalName] = attr.Value.Trim();
        }

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