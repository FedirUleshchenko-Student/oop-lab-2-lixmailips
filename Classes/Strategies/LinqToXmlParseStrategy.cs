using liXMaiLips.Models;
using System.Xml.Linq;

namespace liXMaiLips.Strategies;

public class LinqToXmlParseStrategy : IXmlParseStrategy
{
    public List<XmlRecord> Search(string xmlContent, DynamicSearchParams parameters)
    {
        return new List<XmlRecord>();
    }
}
