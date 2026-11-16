using liXMaiLips.Models;
using liXMaiLips.Services;
using System.Xml;

namespace liXMaiLips.Strategies;

public class SaxParseStrategy : IXmlParseStrategy
{
    private sealed class ElementFrame
    {
        public string Name { get; init; } = string.Empty;
        public Dictionary<string, string> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> LeafChildren { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string TextContent { get; set; } = string.Empty;
        public bool HasChildElements { get; set; }
        public bool HasComplexChildren { get; set; }
    }

    public List<XmlRecord> Search(string xmlContent, DynamicSearchParams parameters)
    {
        var results = new List<XmlRecord>();
        if (string.IsNullOrWhiteSpace(xmlContent))
            return results;

        var settings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        using var stringReader = new StringReader(xmlContent);
        using var reader = XmlReader.Create(stringReader, settings);

        var stack = new Stack<ElementFrame>();

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    {
                        if (stack.Count > 0)
                            stack.Peek().HasChildElements = true;

                        var frame = new ElementFrame { Name = reader.LocalName };
                        bool isEmpty = reader.IsEmptyElement;

                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                            {
                                if (!reader.Name.StartsWith("xmlns") && !string.IsNullOrWhiteSpace(reader.Value))
                                    frame.Attributes[reader.LocalName] = reader.Value.Trim();
                            }
                            reader.MoveToElement();
                        }

                        stack.Push(frame);

                        if (isEmpty)
                            ProcessEndElement(stack, parameters, results);

                        break;
                    }

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    {
                        if (stack.Count > 0 && !string.IsNullOrWhiteSpace(reader.Value))
                            stack.Peek().TextContent += reader.Value;
                        break;
                    }

                case XmlNodeType.EndElement:
                    {
                        if (stack.Count > 0)
                            ProcessEndElement(stack, parameters, results);
                        break;
                    }
            }
        }

        return results;
    }

    private static void ProcessEndElement(
        Stack<ElementFrame> stack,
        DynamicSearchParams parameters,
        List<XmlRecord> results)
    {
        var current = stack.Pop();

        // Root element is never a record
        if (stack.Count == 0)
            return;

        var parent = stack.Peek();

        // Treat any element without child elements (below root level) as a leaf field of its parent
        if (!current.HasChildElements && stack.Count > 1)
        {
            foreach (var (k, v) in current.Attributes)
                AppendFieldValue(parent.LeafChildren, $"{current.Name}.{k}", v);

            if (!string.IsNullOrWhiteSpace(current.TextContent))
                AppendFieldValue(parent.LeafChildren, current.Name, current.TextContent);

            return;
        }

        // Otherwise current is either a record or a grouping container
        parent.HasComplexChildren = true;

        bool isRecord = (!current.HasChildElements && stack.Count == 1)
                        || (current.HasChildElements && !current.HasComplexChildren);

        if (!isRecord)
            return;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Inherit attributes from ancestor containers
        foreach (var ancestor in stack.Reverse())
        {
            foreach (var (k, v) in ancestor.Attributes)
                AppendFieldValue(fields, k, v);
        }

        foreach (var (k, v) in current.Attributes)
            AppendFieldValue(fields, k, v);

        foreach (var (k, v) in current.LeafChildren)
            AppendFieldValue(fields, k, v);

        if (!current.HasChildElements && current.Attributes.Count == 0 && !string.IsNullOrWhiteSpace(current.TextContent))
            AppendFieldValue(fields, current.Name, current.TextContent);

        var record = new XmlRecord { Fields = fields };
        if (record.Fields.Count > 0 && XmlFilterMatcher.Matches(record, parameters))
            results.Add(record);
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