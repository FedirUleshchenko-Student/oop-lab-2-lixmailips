using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace liXMaiLips.Services;

public interface IGoogleDriveAPI
{
    public Task<bool> AuthenticateAsync();

    // for DownloadFileAsync
    // returns Dictionary with file name as key, id - as value
    public Task<Dictionary<string, string>> GetAvailableFilesAsync(List<string> extensions);

    public Task<bool> DownloadFileAsync(string fileId, string targetPath);

    public Task<string> UploadFileAsync(string filePath);
}