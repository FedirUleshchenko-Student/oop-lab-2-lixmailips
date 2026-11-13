using liXMaiLips.Models;

namespace liXMaiLips.Strategies;

public class DomParseStrategy : IXmlParseStrategy
{
    public List<StaffMember> Search(string xmlContent, SearchParams parameters)
    {
        // placeholder
        return new List<StaffMember> { new StaffMember { FullName = "Іванов І.І.", Faculty = "ФКНК" } };
    }
}
