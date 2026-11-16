namespace liXMaiLips.Services;

public class GoogleDriveService : IGoogleDriveAPI
{

    // TODO: Realise GoogleDriveService. All methods should be async

    public Task<bool> AuthenticateAsync()
    {
        return Task.FromResult(false);
    }

    public Task<Dictionary<string, string>> GetAvailableFilesAsync(List<string> extensions)
    {
        return Task.FromResult(new Dictionary<string, string>());
    }

    public Task<Stream?> DownloadFileAsync(string fileId)
    {
        // TODO: Download file into a MemoryStream and reset stream.Position = 0 before returning
        return Task.FromResult<Stream?>(null);
    }

    public Task<string> UploadFileAsync(string filePath)
    {
        return Task.FromResult("");
    }
}
