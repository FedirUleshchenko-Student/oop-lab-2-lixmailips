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

        try
        {
            if (saver.Save(content, localTempPath))
            {
                string id = await _driveApi.UploadFileAsync(localTempPath);
                if (!string.IsNullOrEmpty(id))
                {
                    Logger.GetInstance().Log(LogType.SAVE, $"Збережено файл {fileName} на Google Drive");
                    return true;
                }
            }
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(localTempPath))
                    File.Delete(localTempPath);
            }
            catch
            {
                // Ignore temporary file cleanup errors
            }
        }
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