using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using liXMaiLips.Services; // For AttributeExtractor
using liXMaiLips.UI;

namespace liXMaiLips.Pages;

public partial class MainPage : ContentPage
{
    // Collection for binding values (yellow fields / labels with crosses)
    public ObservableCollection<ParameterItem> ParametersList { get; set; } = new();

    // Collection for scrollable event log
    public ObservableCollection<string> LogEntries { get; set; } = new();

    // Store available attributes from the loaded XML file for argument selection
    private Dictionary<string, List<string>> _availableFileAttributes = new();
    private string _currentLoadedXml = string.Empty;

    public MainPage()
    {
        InitializeComponent();

        // Set context for data binding of values collection in XAML
        BindingContext = this;

        LogAction("Application started successfully");
    }

    private void LogAction(string actionText)
    {
        string logEntry = $"{DateTime.Now:HH:mm:ss} - {actionText}";
        LogEntries.Insert(0, logEntry); // Add to the beginning so new logs appear at the top
    }

    // ==========================================
    // BUTTON HANDLERS AND INTERACTIONS
    // ==========================================

    private async void OnChooseXmlClicked(object sender, EventArgs e)
    {
        LogAction("Selected XML file (locally / Google Drive)");

        // Test XML from which attributes are extracted
        _currentLoadedXml = "<Root><Item fullName='Ivanov I.I.' faculty='FIOT' department='ZI' educationType='Full-time' /><Item fullName='Petrov P.P.' faculty='FPM' department='PM' educationType='Part-time' /></Root>";

        // Extract existing attributes from the file using AttributeExtractor
        var extractor = new AttributeExtractor();
        _availableFileAttributes = extractor.ExtractUniqueAttributes(_currentLoadedXml);

        LblFileInfo.Text = "File short description: sample_data.xml (Loaded)";
        await DisplayAlert("Stub", "XML file read, available arguments updated successfully.", "OK");
    }

    private async void OnChooseTranslateClicked(object sender, EventArgs e)
    {
        LogAction("Selected translation file (translate)");
        await DisplayAlert("Stub", "Translation/localization file connected successfully.", "OK");
    }

    private async void OnAutoTranslateClicked(object sender, EventArgs e)
    {
        LogAction("Auto-detection of localization/translation");
        await DisplayAlert("Stub", "Automatic localization applied.", "OK");
    }

    private void OnCloseFileClicked(object sender, EventArgs e)
    {
        LogAction("Closed file and cleared data");
        LblFileInfo.Text = "File short description: not selected";
        LblSelectedDetails.Text = "Detailed information specified here...";
        ParametersList.Clear();
        _availableFileAttributes.Clear();
        _currentLoadedXml = string.Empty;
    }

    private async void OnChooseXslClicked(object sender, EventArgs e)
    {
        LogAction("Selected XSL template");
        await DisplayAlert("Stub", "XSL template selected.", "OK");
    }

    private async void OnTransformHtmlClicked(object sender, EventArgs e)
    {
        LogAction("Transformation to HTML performed");
        await DisplayAlert("Stub", "Document successfully transformed into HTML format.", "OK");
    }

    private async void OnSaveXmlClicked(object sender, EventArgs e)
    {
        LogAction("Saving XML/HTML file");
        await DisplayAlert("Stub", "File saved successfully (local disk and Google Drive supported).", "OK");
    }

    private void OnClearXslParamsClicked(object sender, EventArgs e)
    {
        LogAction("Cleared XSL parameters");
    }

    private void OnResetAllClicked(object sender, EventArgs e)
    {
        LogAction("Reset all program parameters");
        ParametersList.Clear();
        LblFileInfo.Text = "File short description: not selected";
        _availableFileAttributes.Clear();
    }

    // Remove a specific value from the yellow field (clicking on cross "?")
    private void OnRemoveParamValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string val)
        {
            foreach (var param in ParametersList)
            {
                if (param.Values.Contains(val))
                {
                    param.Values.Remove(val);
                    LogAction($"Removed value '{val}' from parameter '{param.Name}'");
                    break;
                }
            }
        }
    }

    // Add a new parameter (argument) via dropdown (only those present in the file and NOT YET ADDED)
    private async void OnAddParameterClicked(object sender, EventArgs e)
    {
        if (_availableFileAttributes == null || !_availableFileAttributes.Any())
        {
            await DisplayAlert("Warning", "Please load an XML file first to get available arguments from it.", "OK");
            return;
        }

        // Get names of parameters already added to the screen
        var existingNames = ParametersList.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Filter: leave only those not yet added
        var unaddedAttributes = _availableFileAttributes.Keys
            .Where(attr => !existingNames.Contains(attr))
            .ToArray();

        if (!unaddedAttributes.Any())
        {
            await DisplayAlert("Info", "All available arguments from the file are already added to the screen.", "OK");
            return;
        }

        // Invoke dialog (Dropdown via DisplayActionSheet) with available arguments to add
        string selectedAttr = await DisplayActionSheet("Select argument from file", "Cancel", null, unaddedAttributes);

        if (!string.IsNullOrEmpty(selectedAttr) && selectedAttr != "Cancel")
        {
            var newParam = new ParameterItem { Name = selectedAttr };

            // Automatically pull existing unique values for this attribute from the file
            if (_availableFileAttributes.TryGetValue(selectedAttr, out var uniqueValues))
            {
                foreach (var val in uniqueValues)
                {
                    newParam.Values.Add(val);
                }
            }

            ParametersList.Add(newParam);
            LogAction($"Added new parameter-argument: {selectedAttr}");
        }
    }

    // Show/hide input field for a new value in a specific parameter
    private void OnToggleAddValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ParameterItem param)
        {
            param.IsAddingValue = !param.IsAddingValue;
            if (!param.IsAddingValue)
            {
                param.NewValueInput = string.Empty;
            }
        }
    }

    // Save new value to the corresponding parameter
    private void OnSaveNewValueClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is ParameterItem param)
        {
            if (!string.IsNullOrWhiteSpace(param.NewValueInput))
            {
                param.Values.Add(param.NewValueInput.Trim());
                LogAction($"Added value '{param.NewValueInput}' to parameter '{param.Name}'");

                param.NewValueInput = string.Empty;
                param.IsAddingValue = false; // Collapse input field after saving
            }
        }
    }
}