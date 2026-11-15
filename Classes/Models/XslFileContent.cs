namespace liXMaiLips.Models;

public class XslFileContent : FileContent, IFileExtentions
{
    public static List<string> getExtentions() => new List<string> { ".xsl", ".xslt" };

    public new required string? Content
    {
        get => base.Content;
        init => base.Content = value;
    }
}