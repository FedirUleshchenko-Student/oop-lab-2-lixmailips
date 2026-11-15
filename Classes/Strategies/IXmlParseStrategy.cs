using liXMaiLips.Models;

namespace liXMaiLips.Strategies;

public interface IXmlParseStrategy
{
    List<XmlRecord> Search(string xmlContent, DynamicSearchParams parameters);
}