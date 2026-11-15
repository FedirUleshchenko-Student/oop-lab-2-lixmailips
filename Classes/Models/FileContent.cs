namespace liXMaiLips.Models;

public abstract class FileContent
{
    public string? Content { get; protected set; }
    public required FileProperties? Properties { get; init; }
}

public interface IFileExtentions
{
    // static abstract - could be only in interfaces
    static abstract List<string> getExtentions();
}