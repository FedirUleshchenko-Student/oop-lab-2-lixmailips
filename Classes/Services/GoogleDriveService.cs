namespace liXMaiLips.Services;

public class GoogleDriveService : IGoogleDriveAPI
{

    // TODO: Realise GoogleDriveService. All methods should be async

    public Task<bool> AuthenticateAsync()
    {
        return new Task<bool>(() => false);
    }

    public Task<Dictionary<string, string>> GetAvailableFilesAsync(List<string> extensions)
    {
        return new Task<Dictionary<string, string>>(() => new Dictionary<string, string>());
    }

    public Task<bool> DownloadFileAsync(string fileId, string targetPath)
    {
        return new Task<bool>(() => false);
    }

    public Task<string> UploadFileAsync(string filePath)
    {
        return new Task<string>(() => "");
    }
}
