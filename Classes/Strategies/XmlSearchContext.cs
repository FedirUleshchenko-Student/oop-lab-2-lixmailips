using liXMaiLips.Models;
using liXMaiLips.Strategies;

public class XmlSearchContext
{
    private IXmlParseStrategy? _strategy;

    public void SetStrategy(IXmlParseStrategy strategy) => _strategy = strategy;

    public List<StaffMember> ExecuteSearch(string xmlContent, SearchParams parameters)
    {
        return _strategy?.Search(xmlContent, parameters) ?? new List<StaffMember>();
    }
}