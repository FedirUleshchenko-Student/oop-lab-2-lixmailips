namespace liXMaiLips.Factories;

public interface IFileSaver
{
    bool Save(string content, string fileName);
}

public class XmlFileSaver : IFileSaver
{
    public bool Save(string content, string fileName) { File.WriteAllText(fileName, content); return true; }
}

public class HtmlFileSaver : IFileSaver
{
    public bool Save(string content, string fileName) { File.WriteAllText(fileName, content); return true; }
}