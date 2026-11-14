using System.Xml.Linq;
using System.Text;

namespace liXMaiLips.Services
{
    public class AttributeExtractor
    {
        // atribute name -> list of unique values
        public Dictionary<string, List<string>>? ExtractUniqueAttributes(string xmlContent)
        {
            var attributesMap = new Dictionary<string, List<string>>();

            if (string.IsNullOrWhiteSpace(xmlContent))
                return attributesMap;

            try
            {
                XDocument doc = XDocument.Parse(xmlContent);

                foreach (var element in doc.Descendants().Descendants())
                {
                    foreach (var attr in element.Descendants())
                    {
                        string attrName = attr.Name.LocalName;
                        string attrValue = attr.Value;

                        if (!attributesMap.ContainsKey(attrName))
                            attributesMap[attrName] = new List<string>();

                        if (!attributesMap[attrName].Contains(attrValue))
                            attributesMap[attrName].Add(attrValue);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Помилка парсингу атрибутів: {ex.Message}");
                return null;
            }

            // TEMP CODE
            System.Diagnostics.Debug.WriteLine(String.Format("Full attributes : {0}",
                new Func<string>(() => {
                    StringBuilder a = new StringBuilder("");
                    foreach(var x in attributesMap) a.Append($"[{x.Key} : {
                        new Func<List<string>, string>((x) => {
                            StringBuilder a = new StringBuilder("");
                            foreach(var s in x) a.Append(s + " ");
                            return a.ToString();
                        })(x.Value)
                    }] ");
                    return a.ToString();
                })()
                ));

            return attributesMap;
        }
    }
}