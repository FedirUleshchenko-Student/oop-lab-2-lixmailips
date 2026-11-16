using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace liXMaiLips.Services;

public class GoogleDriveService : IGoogleDriveAPI
{
    private const string ApplicationName = "liXMaiLips";
    private const string CredentialsFileName = "credentials.json";
    private static readonly string[] Scopes = { DriveService.Scope.Drive };

    private DriveService? _driveService;

    public async Task<bool> AuthenticateAsync()
    {
        if (_driveService != null)
            return true;

        try
        {
            await using Stream? credentialsStream = await OpenCredentialsStreamAsync();
            if (credentialsStream == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Google Drive error: '{CredentialsFileName}' not found in Resources/Raw or application directory.");
                return false;
            }

            var clientSecrets = await GoogleClientSecrets.FromStreamAsync(credentialsStream);
            string tokenFolderPath = Path.Combine(FileSystem.AppDataDirectory, "GoogleDriveTokens");

            // Timeout prevents indefinite hang if the user closes the browser window without signing in
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user",
                cts.Token,
                new FileDataStore(tokenFolderPath, fullPath: true));

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Drive authentication failed: {ex.Message}");
            _driveService = null;
            return false;
        }
    }

    public async Task<Dictionary<string, string>> GetAvailableFilesAsync(List<string> extensions)
    {
        var filesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (_driveService == null && !await AuthenticateAsync())
            return filesMap;

        try
        {
            var normalizedExtensions = extensions
                .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var request = _driveService!.Files.List();
            request.Q = "trashed = false and mimeType != 'application/vnd.google-apps.folder'";
            request.Fields = "files(id, name, modifiedTime)";
            request.OrderBy = "modifiedTime desc";
            request.PageSize = 100;

            var fileList = await request.ExecuteAsync();
            if (fileList.Files == null)
                return filesMap;

            foreach (var file in fileList.Files)
            {
                if (string.IsNullOrEmpty(file.Name) || string.IsNullOrEmpty(file.Id))
                    continue;

                string fileExt = Path.GetExtension(file.Name);
                if (!normalizedExtensions.Contains(fileExt))
                    continue;

                // Ensure unique display keys if duplicate file names exist in different folders
                string displayKey = file.Name;
                if (filesMap.ContainsKey(displayKey))
                {
                    string shortId = file.Id.Length > 6 ? file.Id[..6] : file.Id;
                    displayKey = $"{file.Name} ({shortId})";
                }

                filesMap[displayKey] = file.Id;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Drive list files error: {ex.Message}");
        }

        return filesMap;
    }

    public async Task<Stream?> DownloadFileAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        if (_driveService == null && !await AuthenticateAsync())
            return null;

        var memoryStream = new MemoryStream();
        try
        {
            var request = _driveService!.Files.Get(fileId);
            var progress = await request.DownloadAsync(memoryStream);

            if (progress.Status != DownloadStatus.Completed)
            {
                await memoryStream.DisposeAsync();
                return null;
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            await memoryStream.DisposeAsync();
            System.Diagnostics.Debug.WriteLine($"Google Drive download error: {ex.Message}");
            return null;
        }
    }

    public async Task<string> UploadFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return string.Empty;

        if (_driveService == null && !await AuthenticateAsync())
            return string.Empty;

        try
        {
            string fileName = Path.GetFileName(filePath);
            string mimeType = GetMimeType(fileName);

            var fileMetadata = new DriveFile
            {
                Name = fileName
            };

            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var request = _driveService!.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id, name";

            var progress = await request.UploadAsync();
            if (progress.Status == UploadStatus.Completed && request.ResponseBody != null)
            {
                return request.ResponseBody.Id ?? string.Empty;
            }

            System.Diagnostics.Debug.WriteLine($"Google Drive upload failed: {progress.Exception?.Message}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Drive upload error: {ex.Message}");
            return string.Empty;
        }
    }

    private static async Task<Stream?> OpenCredentialsStreamAsync()
    {
        // 1. Try MAUI app package asset (Resources/Raw/credentials.json)
        try
        {
            return await FileSystem.OpenAppPackageFileAsync(CredentialsFileName);
        }
        catch
        {
        }

        // 2. Try AppDataDirectory or executable directory
        string[] candidatePaths =
        {
            Path.Combine(FileSystem.AppDataDirectory, CredentialsFileName),
            Path.Combine(AppContext.BaseDirectory, CredentialsFileName)
        };

        foreach (string path in candidatePaths)
        {
            if (File.Exists(path))
                return File.OpenRead(path);
        }

        return null;
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".xml" or ".xsl" or ".xslt" => "application/xml",
            ".html" or ".htm" => "text/html",
            _ => "application/octet-stream"
        };
}