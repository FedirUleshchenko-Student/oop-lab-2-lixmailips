using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace liXMaiLips.Services;

public class GoogleDriveAPI
{
    // ALL METHODS SHOULD BE ASYNC

    public Task<bool> AuthenticateAsync()
    {
        return new Task<bool>(() => false);
    }

    // returns Dictionary with file name as key, id - as value
    // for DownloadFileAsync
    public Task<Dictionary<string, string>> GetAvailableFilesAsync(string extension)
    {
        return new Task<Dictionary<string, string>>(() => new Dictionary<string, string>());
    }

    public Task<string> DownloadFileAsync(string fileId)
    {
        return new Task<string>(() => "");
    }

    public Task<string> UploadFileAsync(string filePath)
    {
        return new Task<string>(() => "");
    }
}