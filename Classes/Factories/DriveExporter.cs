using liXMaiLips.Factories;
using liXMaiLips.Services;
using liXMaiLips.Utils;

public abstract class DriveExporter
{
    protected GoogleDriveAPI _driveApi;
    public DriveExporter(GoogleDriveAPI driveApi) { _driveApi = driveApi; }

    protected abstract IFileSaver CreateSaver();

    // TODO
    public Task<bool> ExportAsync(string content, string fileName)
    {
        return new Task<bool>(() => false);
    }
}

public class XmlDriveExporter : DriveExporter
{
    public XmlDriveExporter(GoogleDriveAPI driveApi) : base(driveApi) { }
    protected override IFileSaver CreateSaver() => new XmlFileSaver();
}

public class HtmlDriveExporter : DriveExporter
{
    public HtmlDriveExporter(GoogleDriveAPI driveApi) : base(driveApi) { }
    protected override IFileSaver CreateSaver() => new HtmlFileSaver();
}