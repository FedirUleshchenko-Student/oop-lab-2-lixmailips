using liXMaiLips.Models;
using liXMaiLips.Strategies;

public class XmlSearchContext
{
    private IXmlParseStrategy? _strategy;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="strategy"></param>
    /// <returns>true - if setted NEW strategy, false - something went wrong or == to old</returns>
    public bool SetStrategy(IXmlParseStrategy strategy)
    {
        bool answer = _strategy?.GetType() == strategy.GetType();
        _strategy = strategy;
        return answer;
    }

    public List<XmlRecord> ExecuteSearch(string xmlContent, DynamicSearchParams parameters)
    {
        return _strategy?.Search(xmlContent, parameters) ?? new List<XmlRecord>();
    }
}