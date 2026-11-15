using liXMaiLips.Factories;
using liXMaiLips.Services;
using liXMaiLips.Utils;

public abstract class DriveExporter
{
    protected GoogleDriveService _driveApi;
    public DriveExporter(GoogleDriveService driveApi) { _driveApi = driveApi; }

    protected abstract IFileSaver CreateSaver();

    public async Task<bool> ExportAsync(string content, string fileName)
    {
        string localTempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
        var saver = CreateSaver();

        if (saver.Save(content, localTempPath))
        {
            string id = await _driveApi.UploadFileAsync(localTempPath);
            Logger.GetInstance().Log(LogType.SAVE, $"Збережено відфільтрований фрагмент у файл {fileName} на Drive");
            return !string.IsNullOrEmpty(id);
        }
        return false;
    }
}

public class XmlDriveExporter : DriveExporter
{
    public XmlDriveExporter(GoogleDriveService driveApi) : base(driveApi) { }
    protected override IFileSaver CreateSaver() => new XmlFileSaver();
}

public class HtmlDriveExporter : DriveExporter
{
    public HtmlDriveExporter(GoogleDriveService driveApi) : base(driveApi) { }
    protected override IFileSaver CreateSaver() => new HtmlFileSaver();
}