namespace TextGrab.Presentation;

public sealed partial class DangerSettingsPage : Page
{
    private bool _isLoading = true;

    public DangerSettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var model = GetModel();
        if (model is null) return;

        _isLoading = true;
        try
        {
            OverrideArchToggle.IsOn = await model.OverrideAiArchCheck;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private DangerSettingsModel? GetModel() =>
        (DataContext as DangerSettingsViewModel)?.Model;

    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private async void ExportBugReportButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = this.GetService<IOptions<AppSettings>>();
        var info = $"Text Grab Uno v5.0\n" +
                   $"Date: {DateTime.Now}\n" +
                   $"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}\n" +
                   $"Arch: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}\n" +
                   $"Settings: {System.Text.Json.JsonSerializer.Serialize(settings?.Value)}";

        ClipboardHelper.CopyText(info);
        await ShowStatusAsync("Bug report copied to clipboard.");
    }

    private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = this.GetService<IOptions<AppSettings>>();
        if (settings?.Value is null) return;

        var json = System.Text.Json.JsonSerializer.Serialize(settings.Value,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var picker = new Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedFileName = "TextGrab-Settings";
        picker.FileTypeChoices.Add("JSON", [".json"]);

#if WINDOWS
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            await Windows.Storage.FileIO.WriteTextAsync(file, json);
            await ShowStatusAsync("Settings exported.");
        }
    }

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".json");

#if WINDOWS
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;

        var json = await Windows.Storage.FileIO.ReadTextAsync(file);
        var imported = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
        if (imported is null)
        {
            await ShowStatusAsync("Invalid settings file.");
            return;
        }

        var writableSettings = this.GetService<global::Uno.Extensions.Configuration.IWritableOptions<AppSettings>>();
        if (writableSettings is not null)
        {
            await writableSettings.UpdateAsync(_ => imported);
            await ShowStatusAsync("Settings imported. Restart app for full effect.");
        }
    }

    private void OverrideArchToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleAiArchOverride();
    }

    private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset All Settings",
            Content = "Are you sure you want to reset all settings to their defaults? This cannot be undone.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (GetModel() is { } model)
            {
                await model.ResetAllSettings();
                await ShowStatusAsync("All settings have been reset to defaults.");
            }
        }
    }

    private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear History",
            Content = "Are you sure you want to delete all history items? This cannot be undone.",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (GetModel() is { } model)
            {
                await model.ClearHistory();
                await ShowStatusAsync("History cleared.");
            }
        }
    }

    private async Task ShowStatusAsync(string message)
    {
        StatusText.Text = message;
        await Task.Delay(3000);
        StatusText.Text = "";
    }
}
