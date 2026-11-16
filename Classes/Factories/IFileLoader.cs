using liXMaiLips.Models;
namespace liXMaiLips.Factories;

public interface ISetUpContent
{
    FileContent SetUp(string content,
            string fileName, string extention, bool isDrive, string fileIdOrPath);
}

public abstract class BaseContentSetUpper : ISetUpContent
{
    public FileContent SetUp(string content,
        string fileName, string extention, bool isDrive, string fileIdOrPath)
    {
        var properties = new FileProperties
        {
            Name = fileName,
            Extention = extention,
            isDrive = isDrive,
            pathOrId = fileIdOrPath
        };

        return SetUp(content, properties);
    }

    // Abstract factory step to be implemented by subclasses
    public abstract FileContent SetUp(string content, FileProperties properties);
}

public class XmlContentSetUpper : BaseContentSetUpper
{
    public override FileContent SetUp(string content, FileProperties properties)
    {
        return new XmlFileContent
        {
            Properties = properties,
            Content = content
        };
    }
}

public class XslContentSetUpper : BaseContentSetUpper
{
    public override FileContent SetUp(string content, FileProperties properties)
    {
        return new XslFileContent
        {
            Properties = properties,
            Content = content
        };
    }
}