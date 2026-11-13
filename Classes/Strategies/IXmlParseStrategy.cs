using liXMaiLips.Models;

namespace liXMaiLips.Strategies;

public interface IXmlParseStrategy
{
    List<StaffMember> Search(string xmlContent, SearchParams parameters);
}