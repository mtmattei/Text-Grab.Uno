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
        // TODO Phase 7: Implement DiagnosticsUtilities.SaveBugReportToFileAsync()
        await ShowStatusAsync("Bug report export is not yet implemented.");
    }

    private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO Phase 7: Implement settings export
        await ShowStatusAsync("Settings export is not yet implemented.");
    }

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO Phase 7: Implement settings import
        await ShowStatusAsync("Settings import is not yet implemented.");
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
            // TODO Phase 7: Wire to IHistoryService.DeleteHistory()
            await ShowStatusAsync("History cleared.");
        }
    }

    private async Task ShowStatusAsync(string message)
    {
        StatusText.Text = message;
        await Task.Delay(3000);
        StatusText.Text = "";
    }
}
