using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using liXMaiLips.Controllers;
using liXMaiLips.Factories;
using liXMaiLips.Models;
using liXMaiLips.Strategies;
using liXMaiLips.UI;

namespace liXMaiLips.Pages;

public partial class MainPage : ContentPage
{
    private readonly MainController _controller = new();

    // Collection for binding search parameter blocks and their tag values
    public ObservableCollection<ParameterItem> ParametersList { get; set; } = new();

    // Collection for scrollable event log
    public ObservableCollection<string> LogEntries { get; set; } = new();

    public MainPage()
    {
        InitializeComponent();

        // Set BindingContext for XAML data bindings
        BindingContext = this;

        // Clear design-time placeholder cards from results container
        ResultsListLayout.Children.Clear();

        // Default strategy selection and automatic re-search on strategy change
        StrategyPicker.SelectedIndex = 0;
        StrategyPicker.SelectedIndexChanged += (s, e) => ExecuteCurrentSearch();

        LogAction("Application started successfully");
    }

    private void LogAction(string actionText)
    {
        string logEntry = $"{DateTime.Now:HH:mm:ss} - {actionText}";
        LogEntries.Insert(0, logEntry);
    }

    private EnumStrategyType GetSelectedStrategy()
    {
        return StrategyPicker.SelectedItem?.ToString() switch
        {
            "LINQ to XML" => EnumStrategyType.LINQ,
            "DOM API" => EnumStrategyType.DOM,
            "SAX API" => EnumStrategyType.SAX,
            _ => EnumStrategyType.LINQ
        };
    }

    // ==========================================
    // FILE & TRANSFORMATION HANDLERS
    // ==========================================

    private async void OnChooseXmlClicked(object sender, EventArgs e)
    {
        string source = await DisplayActionSheet("Джерело XML файлу", "Скасувати", null, "Локальний файл", "Google Drive");
        if (string.IsNullOrEmpty(source) || source == "Скасувати")
            return;

        bool loaded = false;
        var xmlExtensions = XmlFileContent.getExtentions();
        var xmlSetUpper = new XmlContentSetUpper();

        if (source == "Локальний файл")
        {
            loaded = await _controller.LoadLocallyAsync(xmlExtensions, xmlSetUpper);
        }
        else if (source == "Google Drive")
        {
            LogAction("Connecting to Google Drive...");
            bool hasFiles = await _controller.PrepareDriveFilesAsync(xmlExtensions);
            if (!hasFiles)
            {
                await DisplayAlert("Google Drive", "XML файлів не знайдено або помилка авторизації.", "ОК");
                return;
            }

            string[] options = _controller.DriveFilesMap.Keys.ToArray();
            string selectedFile = await DisplayActionSheet("Оберіть XML з Google Drive", "Скасувати", null, options);

            if (!string.IsNullOrEmpty(selectedFile) && selectedFile != "Скасувати")
            {
                loaded = await _controller.LoadDriveFilesAsync(selectedFile, xmlSetUpper);
            }
        }

        if (loaded)
        {
            ParametersList.Clear();
            string fileName = _controller.xmlFile?.Properties?.Name ?? "unknown.xml";
            int attrCount = _controller.CurrentAttributes?.Count ?? 0;

            LblSelectedDetails.Text = "Оберіть елемент зі списку нижче для детального перегляду.";
            LblFileInfo.Text = $"Файл: {fileName} (Атрибутів: {attrCount})";
            UpdateFileInfoLabel();
            LogAction($"Loaded XML: {fileName} ({source})");

            // Run initial search without filters to display all records
            ExecuteCurrentSearch();
        }
    }

    private async void OnChooseTranslateClicked(object sender, EventArgs e)
    {
        LogAction("Selected translation file (translate)");
        await DisplayAlert("Локалізація", "Підключення словника перекладу атрибутів.", "ОК");
    }

    private async void OnAutoTranslateClicked(object sender, EventArgs e)
    {
        LogAction("Auto-detection of localization/translation");
        await DisplayAlert("Локалізація", "Автоматичний переклад назв атрибутів застосовано.", "ОК");
    }

    private void OnCloseFileClicked(object sender, EventArgs e)
    {
        if (_controller.ClearXmlData())
        {
            ParametersList.Clear();
            ResultsListLayout.Children.Clear();
            UpdateFileInfoLabel();
            LblFileInfo.Text = "Короткий опис файлу: не обрано";
            LblSelectedDetails.Text = "Тут вказана детальна інформація про нього...";
            LogAction("Closed XML file and cleared workspace");
        }
    }

    private async void OnChooseXslClicked(object sender, EventArgs e)
    {
        string source = await DisplayActionSheet("Джерело XSL шаблону", "Скасувати", null, "Локальний файл", "Google Drive");
        if (string.IsNullOrEmpty(source) || source == "Скасувати")
            return;

        bool loaded = false;
        var xslExtensions = XslFileContent.getExtentions();
        var xslSetUpper = new XslContentSetUpper();

        if (source == "Локальний файл")
        {
            loaded = await _controller.LoadLocallyAsync(xslExtensions, xslSetUpper);
        }
        else if (source == "Google Drive")
        {
            bool hasFiles = await _controller.PrepareDriveFilesAsync(xslExtensions);
            if (!hasFiles)
            {
                await DisplayAlert("Google Drive", "XSL файлів не знайдено.", "ОК");
                return;
            }

            string[] options = _controller.DriveFilesMap.Keys.ToArray();
            string selectedFile = await DisplayActionSheet("Оберіть XSL з Google Drive", "Скасувати", null, options);

            if (!string.IsNullOrEmpty(selectedFile) && selectedFile != "Скасувати")
            {
                loaded = await _controller.LoadDriveFilesAsync(selectedFile, xslSetUpper);
            }
        }

        if (loaded)
        {
            string xslName = _controller.xslFile?.Properties?.Name ?? "template.xsl";
            UpdateFileInfoLabel();
            LogAction($"Loaded XSL template: {xslName} ({source})");
            await DisplayAlert("XSL", "XSL шаблон успішно завантажено.", "ОК");
        }
    }

    private async void OnTransformHtmlClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_controller.xmlFile?.Content) || string.IsNullOrEmpty(_controller.xslFile?.Content))
        {
            await DisplayAlert("Помилка", "Для трансформації необхідно завантажити як XML-файл, так і XSL-шаблон.", "ОК");
            return;
        }

        string target = await DisplayActionSheet("Куди зберегти HTML?", "Скасувати", null, "Локально (Документи)", "Google Drive");
        if (string.IsNullOrEmpty(target) || target == "Скасувати")
            return;

        string fileName = await DisplayPromptAsync("Назва файлу", "Введіть ім'я вихідного HTML файлу:", initialValue: "result.html");
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (!fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            fileName += ".html";

        bool toDrive = target == "Google Drive";
        bool success = await _controller.TransformToHtmlAsync(fileName, toDrive);

        if (success)
        {
            LogAction($"Transformed to HTML: {fileName} ({target})");
            await DisplayAlert("Успіх", $"Файл {fileName} успішно створено!", "ОК");
        }
        else
        {
            LogAction("HTML transformation failed");
            await DisplayAlert("Помилка", "Не вдалося виконати XSLT-трансформацію.", "ОК");
        }
    }

    private async void OnSaveXmlClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_controller.xmlFile?.Content))
        {
            await DisplayAlert("Помилка", "Немає завантаженого XML для збереження.", "ОК");
            return;
        }

        string target = await DisplayActionSheet("Куди зберегти XML?", "Скасувати", null, "Локально (Документи)", "Google Drive");
        if (string.IsNullOrEmpty(target) || target == "Скасувати")
            return;

        string fileName = await DisplayPromptAsync("Назва файлу", "Введіть ім'я XML файлу:", initialValue: "filtered_result.xml");
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            fileName += ".xml";

        bool toDrive = target == "Google Drive";
        bool success = await _controller.SaveXmlResult(fileName, toDrive);

        if (success)
        {
            LogAction($"Saved XML: {fileName} ({target})");
            await DisplayAlert("Успіх", $"Файл {fileName} успішно збережено!", "ОК");
        }
        else
        {
            LogAction("XML save failed");
            await DisplayAlert("Помилка", "Не вдалося зберегти XML файл.", "ОК");
        }
    }

    private void OnClearXslParamsClicked(object sender, EventArgs e)
    {
        if (_controller.ClearXsl())
        {
            UpdateFileInfoLabel();
            LogAction("Cleared XSL template");
        }
    }

    private void OnResetAllClicked(object sender, EventArgs e)
    {
        _controller.ClearParams();
        ParametersList.Clear();
        LogAction("Reset all search parameters");
        ExecuteCurrentSearch();
    }

    // ==========================================
    // DYNAMIC SEARCH PARAMETERS HANDLERS
    // ==========================================

    // Remove a specific value tag from a parameter and update search results
    private void OnRemoveParamValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string val)
        {
            foreach (var param in ParametersList)
            {
                if (param.Values.Contains(val))
                {
                    param.Values.Remove(val);

                    var delta = CreateSingleParamDelta(param.Name, val);
                    var results = _controller.RemoveParamsAndSearch(delta, GetSelectedStrategy());

                    LogAction($"Removed value '{val}' from '{param.Name}' (found: {results.Count})");
                    RenderResults(results);
                    break;
                }
            }
        }
    }

    // Add a new parameter argument from the loaded XML attributes
    private async void OnAddParameterClicked(object sender, EventArgs e)
    {
        if (_controller.CurrentAttributes == null || !_controller.CurrentAttributes.Any())
        {
            await DisplayAlert("Увага", "Спочатку завантажте XML-файл, щоб отримати доступні аргументи.", "ОК");
            return;
        }

        var existingNames = ParametersList.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unaddedAttributes = _controller.CurrentAttributes.Keys
            .Where(attr => !existingNames.Contains(attr))
            .ToArray();

        if (!unaddedAttributes.Any())
        {
            await DisplayAlert("Інфо", "Усі доступні з файлу аргументи вже додані.", "ОК");
            return;
        }

        string selectedAttr = await DisplayActionSheet("Оберіть аргумент із файлу", "Скасувати", null, unaddedAttributes);

        if (!string.IsNullOrEmpty(selectedAttr) && selectedAttr != "Скасувати")
        {
            var newParam = new ParameterItem { Name = selectedAttr };
            ParametersList.Add(newParam);

            // Register attribute presence filter immediately via DynamicSearchParams constructor
            var delta = new DynamicSearchParams(selectedAttr);

            var results = _controller.AddParamsAndSearch(delta, GetSelectedStrategy());
            LogAction($"Added parameter argument: {selectedAttr} (found: {results.Count})");
            RenderResults(results);
        }
    }

    // Offer existing file values for the selected attribute or toggle manual entry
    private async void OnToggleAddValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ParameterItem param)
        {
            if (param.IsAddingValue)
            {
                param.IsAddingValue = false;
                param.NewValueInput = string.Empty;
                return;
            }

            if (_controller.CurrentAttributes != null &&
                _controller.CurrentAttributes.TryGetValue(param.Name, out var fileValues) &&
                fileValues.Any())
            {
                var availableValues = fileValues
                    .Where(v => !param.Values.Contains(v))
                    .Append("Ввести вручну...")
                    .ToArray();

                string choice = await DisplayActionSheet($"Значення для '{param.Name}'", "Скасувати", null, availableValues);

                if (string.IsNullOrEmpty(choice) || choice == "Скасувати")
                    return;

                if (choice != "Ввести вручну...")
                {
                    param.Values.Add(choice);

                    var delta = CreateSingleParamDelta(param.Name, choice);
                    var results = _controller.AddParamsAndSearch(delta, GetSelectedStrategy());

                    LogAction($"Added value '{choice}' to '{param.Name}' (found: {results.Count})");
                    RenderResults(results);
                    return;
                }
            }

            // Fallback to manual text Entry
            param.IsAddingValue = true;
        }
    }

    // Save manually entered value and trigger controller search
    private void OnSaveNewValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ParameterItem param)
        {
            if (!string.IsNullOrWhiteSpace(param.NewValueInput))
            {
                string trimmed = param.NewValueInput.Trim();
                if (!param.Values.Contains(trimmed))
                {
                    param.Values.Add(trimmed);

                    var delta = CreateSingleParamDelta(param.Name, trimmed);
                    var results = _controller.AddParamsAndSearch(delta, GetSelectedStrategy());

                    LogAction($"Added value '{trimmed}' to '{param.Name}' (found: {results.Count})");
                    RenderResults(results);
                }

                param.NewValueInput = string.Empty;
                param.IsAddingValue = false;
            }
        }
    }

    // ==========================================
    // SEARCH EXECUTION & DYNAMIC UI RENDERING
    // ==========================================

    private DynamicSearchParams CreateSingleParamDelta(string key, string value) =>
        new(key, value);

    private void ExecuteCurrentSearch()
    {
        if (string.IsNullOrEmpty(_controller.xmlFile?.Content))
            return;

        var strategy = GetSelectedStrategy();
        var results = _controller.Search(strategy);

        LogAction($"Search ({strategy}): found {results.Count} items");
        RenderResults(results);
    }

    private void RenderResults(List<XmlRecord> results)
    {
        ResultsListLayout.Children.Clear();

        if (!results.Any())
        {
            ResultsListLayout.Children.Add(new Label
            {
                Text = "За заданими параметрами нічого не знайдено.",
                TextColor = Colors.Gray,
                Margin = new Thickness(10)
            });
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            var record = results[i];

            var card = new Border
            {
                StrokeThickness = 1.5,
                Stroke = Color.FromArgb("#3B5BDB"),
                Padding = new Thickness(12),
                BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#2C3034")
                    : Colors.White,
                StrokeShape = new RoundRectangle { CornerRadius = 6 }
            };

            // Build dynamic summary from available record fields
            var firstField = record.Fields.FirstOrDefault();
            string headerText = !string.IsNullOrEmpty(firstField.Key)
                ? $"{i + 1}. {firstField.Value}"
                : $"{i + 1}. Запис #{i + 1}";

            string summaryText = string.Join(" | ",
                record.Fields.Skip(1).Take(3).Select(kv => $"{kv.Key}: {kv.Value}"));

            var stack = new VerticalStackLayout { Spacing = 4 };
            stack.Children.Add(new Label
            {
                Text = headerText,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3B5BDB")
            });

            if (!string.IsNullOrEmpty(summaryText))
            {
                stack.Children.Add(new Label
                {
                    Text = summaryText,
                    FontSize = 12,
                    TextColor = Colors.Gray
                });
            }

            card.Content = stack;

            // Display all fields of the clicked record in the details panel
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                LblSelectedDetails.Text = string.Join("\n",
                    record.Fields.Select(kv => $"{kv.Key}: {kv.Value}"));
            };
            card.GestureRecognizers.Add(tapGesture);

            ResultsListLayout.Children.Add(card);
        }
    }

    private void UpdateFileInfoLabel()
    {
        string xmlPart = _controller.xmlFile?.Properties?.Name != null
            ? $"XML: {_controller.xmlFile.Properties.Value.Name} (Атрибутів: {_controller.CurrentAttributes?.Count ?? 0})"
            : "XML: не обрано";

        string xslPart = _controller.xslFile?.Properties?.Name != null
            ? $"XSL: {_controller.xslFile.Properties.Value.Name}"
            : "XSL: не обрано";

        LblFileInfo.Text = $"{xmlPart} | {xslPart}";
    }
}