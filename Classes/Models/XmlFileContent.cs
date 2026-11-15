namespace liXMaiLips.Models;

public class XmlFileContent : FileContent, IFileExtentions
{
    public static List<string> getExtentions() => new List<string> { ".xml" };

    //protected string? contentSaved;
    //public string? ContentWithoutParams
    //{
    //    get => contentSaved;
    //    internal set
    //    {
    //        contentSaved = value;
    //        CurrentContent = value;
    //    }
    //}

    //public string? CurrentContent
    //{
    //    get => Content;
    //    internal set => Content = value;
    //}

    //public void RestartContent()
    //{
    //    Content = ContentWithoutParams;
    //}

    public new required string? Content
    {
        get => base.Content;
        init => base.Content = value;
    }
}