using liXMaiLips.Services;
using liXMaiLips.Strategies;
using liXMaiLips.Models;
using liXMaiLips.Utils;
using liXMaiLips.Factories;

namespace liXMaiLips.Controllers;

public class MainController
{
    private readonly GoogleDriveAPI _driveApi = new();
    private readonly XmlSearchContext _searchContext = new();

    // null - if no file has been opened
    public string? CurrentXmlContent { get; private set; }
    public string? CurrentXmlContentWithoutSearchParams { get; private set; }

    public Dictionary<string, string> DriveFilesMap { get; private set; } = new();

    public async Task<bool> PrepareDriveFilesAsync(string extension = ".xml")
    {
        if (await _driveApi.AuthenticateAsync())
        {
            DriveFilesMap = await _driveApi.GetAvailableFilesAsync(extension);
            return DriveFilesMap.Any();
        }
        return false;
    }

    public async Task<bool> LoadXmlFromDriveAsync(string fileName)
    {
        if (DriveFilesMap.TryGetValue(fileName, out string? fileId))
        {
            SetXmlContent(await _driveApi.DownloadFileAsync(fileId));
            return true;
        }
        return false;
    }

    public async Task<bool> LoadXmlLocallyAsync()
    {
        var xmlFileType = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                    // only 1 because of only possible work on windows
                    // could add more options for more platforms
                    
                    // gives 3 (not 2) options because of some sort of MAUI bug
                    { DevicePlatform.WinUI, new[] { ".xml", "*" } }
            });

        var result = await FilePicker.Default.PickAsync(
            new PickOptions { PickerTitle = "Оберіть XML", FileTypes = xmlFileType });

        if (result != null)
        {
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            SetXmlContent(await reader.ReadToEndAsync());
            return true;
        }
        return false;
    }

    public List<StaffMember> Search(SearchParams paramsObj, string strategyType)
    {
        IXmlParseStrategy strategy = strategyType switch
        {
            "LINQ" => new LinqToXmlParseStrategy(),
            "SAX" => new SaxParseStrategy(),
            "DOM" => new DomParseStrategy(),
            _ => new LinqToXmlParseStrategy()
        };

        _searchContext.SetStrategy(strategy);
        var results = _searchContext.ExecuteSearch(CurrentXmlContent, paramsObj);

        Logger.GetInstance().Log(LogType.FILTER, $"Фільтрація за параметрами {paramsObj.ToString()}. Знайдено: {results.Count}");
        return results;
    }

    public async Task<bool> SaveXmlResult(string fileName, bool toDrive = false)
    {
        bool result;
        if(!toDrive)
            result = new XmlFileSaver().Save(CurrentXmlContent ?? "", fileName);
        else
            result = await new XmlDriveExporter(_driveApi).ExportAsync(CurrentXmlContent ?? "", fileName);
        return result;
    }

    public void ClearParams()
    {
        CurrentXmlContent = CurrentXmlContentWithoutSearchParams;
        Logger.GetInstance().Log(LogType.CLEAR, "Очищено параметри та вивід");
    }

    private void SetXmlContent(string text)
    {
        CurrentXmlContentWithoutSearchParams = text;
        CurrentXmlContent = text;
    }
}