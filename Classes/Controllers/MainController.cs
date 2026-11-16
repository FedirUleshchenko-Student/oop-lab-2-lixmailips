using liXMaiLips.Factories;
using liXMaiLips.Models;
using liXMaiLips.Services;
using liXMaiLips.Strategies;
using liXMaiLips.Utils;
using System.Net.Http;
using System.Xml.Linq;

namespace liXMaiLips.Controllers;

public class MainController
{
    #region Services
    private readonly IGoogleDriveAPI _driveApi = new GoogleDriveService();
    private readonly XmlSearchContext _searchContext = new();
    private readonly AttributeExtractor _attributeExtractor = new();
    private readonly XmlTransformationService _transformationService = new();
    #endregion

    // null - not set
    public XmlFileContent? xmlFile { get; private set; }
    public XslFileContent? xslFile { get; private set; }

    public Dictionary<string, List<string>>? CurrentAttributes { get; private set; }
    public DynamicSearchParams CurrentSearchParams { get; private set; } = new();
    #region The Hel - Temp
    public Dictionary<string, string> DriveFilesMap { get; private set; } = new();
    //public List<XmlRecord>? SearchResults { get; private set; }
    #endregion

    private bool _isLastActionXmlSave;

    /// <summary>
    /// Returns true if no XML file is open or if the last action was saving the XML file.
    /// </summary>
    public bool CanCloseSafely() => xmlFile == null || _isLastActionXmlSave;

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

            if (!string.IsNullOrWhiteSpace(content))
            {
                if (!ApplyLoadedContent(
                    setUpper.SetUp(content, fileName, Path.GetExtension(fileName), true, fileId))
                    )
                    return false;

                DriveFilesMap.Clear();
                return true;
            }
        }
        return false;
    }

    public async Task<bool> LoadLocallyAsync(List<string> extensions, ISetUpContent setUpper)
    {
        var fileType = new FilePickerFileType(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, extensions }
            });

        var result = await FilePicker.Default.PickAsync(
            new PickOptions { PickerTitle = "Оберіть потрібний файл", FileTypes = fileType });

        if (result != null)
        {
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            string content = await reader.ReadToEndAsync();

            return ApplyLoadedContent(
                setUpper.SetUp(
                    content,
                    result.FileName,
                    Path.GetExtension(result.FileName),
                    false,
                    result.FullPath)
                );
        }
        return false;
    }

    #region Params management & Search
    public List<XmlRecord> AddParamsAndSearch(DynamicSearchParams newParams, EnumStrategyType? strategyType)
    {
        CurrentSearchParams.Add(newParams);

        var searchResults = Search(strategyType);

        Logger.GetInstance().Log(LogType.FILTER,
            $"Фільтрація з доданими параметрами {newParams} [Стратегія = {strategyType}]. Знайдено: {searchResults.Count}");
        _isLastActionXmlSave = false;

        return searchResults;
    }

    public List<XmlRecord> RemoveParamsAndSearch(DynamicSearchParams newParams, EnumStrategyType? strategyType)
    {
        CurrentSearchParams.Remove(newParams);

        var searchResults = Search(strategyType);

        Logger.GetInstance().Log(LogType.FILTER,
            $"Фільтрація з прибраними параметрами {newParams} [Стратегія = {strategyType}]. Знайдено: {searchResults.Count}");
        _isLastActionXmlSave = false;

        return searchResults;
    }

    public List<XmlRecord> Search(EnumStrategyType? strategyType)
    {
        if (string.IsNullOrEmpty(xmlFile?.Content))
            return new List<XmlRecord>();

        IXmlParseStrategy strategy = strategyType switch
        {
            EnumStrategyType.LINQ => new LinqToXmlParseStrategy(),
            EnumStrategyType.SAX => new SaxParseStrategy(),
            EnumStrategyType.DOM => new DomParseStrategy(),
            _ => new LinqToXmlParseStrategy()
        };

        _searchContext.SetStrategy(strategy);
        return _searchContext.ExecuteSearch(xmlFile.Content, CurrentSearchParams);
    }
    #endregion

    #region Converters & Exporters
    public async Task<bool> TransformToHtmlAsync(string fileName, bool toDrive = false)
    {
        if (string.IsNullOrWhiteSpace(xmlFile?.Content) || string.IsNullOrWhiteSpace(xslFile?.Content))
            return false;

        try
        {
            string targetXml = _transformationService.BuildFilteredXml(xmlFile.Content, CurrentSearchParams);
            string htmlContent = _transformationService.TransformToHtml(targetXml, xslFile.Content);

            if (toDrive)
            {
                if (_driveApi is GoogleDriveService GDS)
                {
                    var exporter = new HtmlDriveExporter(GDS);
                    return await exporter.ExportAsync(htmlContent, fileName);
                }
                else return false;
            }

            string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fullPath = Path.Combine(documentsDir, fileName);
            IFileSaver saver = new HtmlFileSaver();

            if (saver.Save(htmlContent, fullPath))
            {
                Logger.GetInstance().Log(LogType.SAVE, $"Збережено HTML файл: {fullPath}");
                _isLastActionXmlSave = false;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTML transformation error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SaveXmlResult(string fileName, bool toDrive = false)
    {
        if (string.IsNullOrWhiteSpace(xmlFile?.Content))
            return false;

        try
        {
            string filteredXml = _transformationService.BuildFilteredXml(xmlFile.Content, CurrentSearchParams);

            if (toDrive)
            {
                if (_driveApi is GoogleDriveService GDS)
                {
                    var exporter = new XmlDriveExporter(GDS);
                    return await exporter.ExportAsync(filteredXml, fileName);
                }
                else return false;
            }

            string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string fullPath = Path.Combine(documentsDir, fileName);
            IFileSaver saver = new XmlFileSaver();

            if (saver.Save(filteredXml, fullPath))
            {
                Logger.GetInstance().Log(LogType.SAVE, $"Збережено XML файл: {fullPath}");
                _isLastActionXmlSave = true;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"XML save error: {ex.Message}");
            return false;
        }
    }
    #endregion

    #region Clearing
    /// <returns>Returns false if there is nothing to clear.</returns>
    public bool ClearParams()
    {
        if (CurrentSearchParams == null || CurrentSearchParams.IsEmpty())
            return false;

        CurrentSearchParams.Clear();

        Logger.GetInstance().Log(LogType.CLEAR, "Очищено параметри та вивід");
        _isLastActionXmlSave = false;
        return true;
    }

    /// <returns>Returns false if there is nothing to clear.</returns>
    public bool ClearXsl()
    {
        if (xslFile == null)
            return false;

        xslFile = null;

        Logger.GetInstance().Log(LogType.CLEAR, "Очищено XSL шаблон");
        _isLastActionXmlSave = false;
        return true;
    }

    /// <returns>Returns false if there is nothing to clear.</returns>
    public bool ClearXmlData()
    {
        if (xmlFile == null)
            return false;

        xmlFile = null;
        CurrentAttributes = null;
        CurrentSearchParams.Clear();

        Logger.GetInstance().Log(LogType.CLEAR, "Закрито XML файл та очищено дані");
        _isLastActionXmlSave = false;
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

    private bool ApplyLoadedContent(FileContent content)
    {
        if (string.IsNullOrWhiteSpace(content.Content))
            return false;

        try
        {
            // Validate well-formed XML structure for both XML and XSL documents
            XDocument.Parse(content.Content);
        }
        catch
        {
            return false;
        }

        _isLastActionXmlSave = false;
        if (content is XmlFileContent xml)
        {
            xmlFile = xml;
            RefreshAttributes();
            return true;
        }

        if (content is XslFileContent xsl)
        {
            xslFile = xsl;
            return true;
        }

        return false;
    }
}