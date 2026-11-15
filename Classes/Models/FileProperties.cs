namespace liXMaiLips.Models;

public struct FileProperties
{
    public required string Name { get; init; }
    public required string Extention { get; init; }

    public required bool isDrive { get; init; }
    public required string pathOrId { get; init; }
}
