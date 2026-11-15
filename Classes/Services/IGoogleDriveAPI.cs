using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace liXMaiLips.Services;

public interface IGoogleDriveAPI
{
    public Task<bool> AuthenticateAsync();

    /// <summary>
    /// for DownloadFileAsync
    /// </summary>
    /// <param name="extensions"></param>
    /// <returns>returns Dictionary with file name as key, id - as value</returns>
    public Task<Dictionary<string, string>> GetAvailableFilesAsync(List<string> extensions);

    // Downloads file directly into a memory stream (returns null on failure)
    public Task<Stream?> DownloadFileAsync(string fileId);

    public Task<string> UploadFileAsync(string filePath);
}