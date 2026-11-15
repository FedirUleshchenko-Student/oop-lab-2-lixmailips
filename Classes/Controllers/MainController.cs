using liXMaiLips.Factories;
using liXMaiLips.Models;
using liXMaiLips.Services;
using liXMaiLips.Strategies;
using liXMaiLips.Utils;

namespace liXMaiLips.Controllers;

public class MainController
{
    #region Services
    private readonly IGoogleDriveAPI _driveApi = new GoogleDriveService();
    private readonly XmlSearchContext _searchContext = new();
    private readonly AttributeExtractor _attributeExtractor = new();
    #endregion

    // null - not set
    public XmlFileContent? xmlFile {  get; private set; }
    public XslFileContent? xslFile {  get; private set; }

    public Dictionary<string, List<string>>? CurrentAttributes { get; private set; }
    public DynamicSearchParams CurrentSearchParams { get; private set; } = new DynamicSearchParams();

    #region The Hel - Temp
    public Dictionary<string, string> DriveFilesMap { get; private set; } = new();
    //public List<XmlRecord>? SearchResults { get; private set; }
    #endregion

    public async Task<bool> PrepareDriveFilesAsync(List<string> extensions)
    {
        if (await _driveApi.AuthenticateAsync())
        {
            DriveFilesMap = await _driveApi.GetAvailableFilesAsync(extensions);
            return DriveFilesMap.Any();
        }
        return false;
    }

    public async Task<bool> LoadDriveFilesAsync(string fileName, ISetUpContent setUpper)
    {
        if (DriveFilesMap.TryGetValue(fileName, out string? fileId))
        {
            await using Stream? stream = await _driveApi.DownloadFileAsync(fileId);
            if (stream == null)
                return false;

            using var reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync();

            if (!string.IsNullOrEmpty(content))
            {
                ApplyLoadedContent(setUpper.SetUp(content, fileName, Path.GetExtension(fileName), true, fileId));

                DriveFilesMap.Clear();
                return true;
            }
        }
        return false;
    }

    public async Task<bool> LoadLocallyAsync(List<string> extentions, ISetUpContent setUpper)
    {
        var fileType = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
                    // only 1 because of only possible work on windows
                    // could add more options for more platforms
                
                    // gives 3 (not 2) options because of some sort of MAUI bug
                    { DevicePlatform.WinUI, extentions }
        });

        var result = await FilePicker.Default.PickAsync(
            new PickOptions { PickerTitle = "Оберіть потрібний файл", FileTypes = fileType });

        if (result != null)
        {
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);

            ApplyLoadedContent(
                setUpper.SetUp(
                    await reader.ReadToEndAsync(),
                    result.FileName, Path.GetExtension(result.FileName), false, result.FullPath)
                );

            return true;
        }
        return false;
    }

    #region Params management & Search
    public List<XmlRecord> AddParamsAndSearch(DynamicSearchParams newParams, EnumStrategyType? strategyType)
    {
        CurrentSearchParams.Add(newParams);

        var SearchResults = Search(strategyType);

        Logger.GetInstance().Log(LogType.FILTER,
            $"Фільтрація з доданими параметрами {newParams} [Стратегія = {strategyType}]. Знайдено: {SearchResults.Count}");

        return SearchResults;
    }

    public List<XmlRecord> RemoveParamsAndSearch(DynamicSearchParams newParams, EnumStrategyType? strategyType)
    {
        CurrentSearchParams.Remove(newParams);

        var SearchResults = Search(strategyType);

        Logger.GetInstance().Log(LogType.FILTER,
            $"Фільтрація з прибраними параметрами {newParams} [Стратегія = {strategyType}]. Знайдено: {SearchResults.Count}");

        return SearchResults;
    }

    public List<XmlRecord> Search(EnumStrategyType? strategyType)
    {
        if (string.IsNullOrEmpty(xmlFile?.Content))
            return new List<XmlRecord>();

        var SearchResults = new List<XmlRecord>();

        IXmlParseStrategy strategy = strategyType switch
        {
            EnumStrategyType.LINQ => new LinqToXmlParseStrategy(),
            EnumStrategyType.SAX => new SaxParseStrategy(),
            EnumStrategyType.DOM => new DomParseStrategy(),
            _ => new LinqToXmlParseStrategy()
        };

        // ???
        //
        //if (_searchContext.SetStrategy(strategy))
        //    CurrentSearchParams.Clear();

        _searchContext.SetStrategy(strategy);
        return _searchContext.ExecuteSearch(xmlFile.Content, CurrentSearchParams);
    }
    #endregion

    #region Converters
    // TODO: Saviors of HTML & XML files

    public async Task<bool> TransformToHtmlAsync(string fileName, bool toDrive = false)
    {
        return true;
    }

    public async Task<bool> SaveXmlResult(string fileName, bool toDrive = false)
    {
        return true;
    }
    #endregion

    #region Clearing
    /// <returns>return false if there is nothing to clear</returns>
    public bool ClearParams()
    {
        if (CurrentSearchParams == null
            || CurrentSearchParams.IsEmpty()) return false;
        CurrentSearchParams.Clear();
        Logger.GetInstance().Log(LogType.CLEAR, "Очищено параметри та вивід");
        return true;
    }

    /// <returns>return false if there is nothing to clear</returns>
    public bool ClearXsl()
    {
        if (xslFile == null) return false;
        xslFile = null;
        Logger.GetInstance().Log(LogType.CLEAR, "Очищено XSL шаблон");
        return true;
    }

    /// <returns>return false if there is nothing to clear</returns>
    public bool ClearXmlData()
    {
        if (xmlFile == null) return false;
        xmlFile = null;
        CurrentAttributes = null;
        CurrentSearchParams.Clear();
        Logger.GetInstance().Log(LogType.CLEAR, "Закрито XML файл та очищено дані");
        return true;
    }
    #endregion

    private void RefreshAttributes()
    {
        CurrentAttributes = string.IsNullOrEmpty(xmlFile?.Content)
            ? new Dictionary<string, List<string>>()
            : _attributeExtractor.ExtractUniqueAttributes(xmlFile.Content);
        CurrentSearchParams.Clear();
    }

    private void ApplyLoadedContent(FileContent content)
    {
        if (content is XmlFileContent xml)
        {
            xmlFile = xml;
            RefreshAttributes();
        }
        else if (content is XslFileContent xsl)
        {
            xslFile = xsl;
        }
    }
}