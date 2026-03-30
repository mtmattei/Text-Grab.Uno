namespace TextGrab.Presentation;

public sealed partial class GeneralSettingsPage : Page
{
    private bool _isLoading = true;

    public GeneralSettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var model = (DataContext as GeneralSettingsViewModel)?.Model;
        if (model is null) return;

        // Set initial toggle states from model
        InitializeAsync(model);

#if WINDOWS
        StartupOnLoginToggle.Visibility = Visibility.Visible;
#endif
    }

    private async void InitializeAsync(GeneralSettingsModel model)
    {
        _isLoading = true;

        try
        {
            // Theme
            var theme = await model.AppTheme ?? "System";
            for (int i = 0; i < ThemeRadioButtons.Items.Count; i++)
            {
                if (ThemeRadioButtons.Items[i] is RadioButton rb && rb.Tag?.ToString() == theme)
                {
                    ThemeRadioButtons.SelectedIndex = i;
                    break;
                }
            }

            // Default launch
            var launch = await model.DefaultLaunch ?? "EditText";
            for (int i = 0; i < DefaultLaunchRadioButtons.Items.Count; i++)
            {
                if (DefaultLaunchRadioButtons.Items[i] is RadioButton rb && rb.Tag?.ToString() == launch)
                {
                    DefaultLaunchRadioButtons.SelectedIndex = i;
                    break;
                }
            }

            ShowToastToggle.IsOn = await model.ShowToast;
            RunInBackgroundToggle.IsOn = await model.RunInTheBackground;
            ReadBarcodesToggle.IsOn = await model.ReadBarcodesOnGrab;
            ErrorCorrectToggle.IsOn = await model.CorrectErrors;
            CorrectToLatinToggle.IsOn = await model.CorrectToLatin;
            NeverClipboardToggle.IsOn = await model.NeverAutoUseClipboard;
            TryInsertToggle.IsOn = await model.TryInsert;
            HistoryToggle.IsOn = await model.UseHistory;

#if WINDOWS
            StartupOnLoginToggle.IsOn = await model.StartupOnLogin;
#endif

            // Web search
            var url = await model.WebSearchUrl ?? "";
            for (int i = 0; i < WebSearchComboBox.Items.Count; i++)
            {
                if (WebSearchComboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == url)
                {
                    WebSearchComboBox.SelectedIndex = i;
                    break;
                }
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private GeneralSettingsModel? GetModel() =>
        (DataContext as GeneralSettingsViewModel)?.Model;

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        if (ThemeRadioButtons.SelectedItem is RadioButton rb && rb.Tag is string theme)
            _ = model.SetTheme(theme);
    }

    private void DefaultLaunchRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        if (DefaultLaunchRadioButtons.SelectedItem is RadioButton rb && rb.Tag is string launch)
            _ = model.SetDefaultLaunch(launch);
    }

    private void ShowToastToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleShowToast();
    }

    private void RunInBackgroundToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleRunInBackground();
    }

    private void StartupOnLoginToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleStartupOnLogin();
    }

    private void ReadBarcodesToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleBarcodes();
    }

    private void WebSearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        if (WebSearchComboBox.SelectedItem is ComboBoxItem item && item.Tag is string url)
            _ = model.SetWebSearchUrl(url);
    }

    private void ErrorCorrectToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleCorrectErrors();
    }

    private void CorrectToLatinToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleCorrectToLatin();
    }

    private void NeverClipboardToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleNeverAutoClipboard();
    }

    private void TryInsertToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleTryInsert();
    }

    private void HistoryToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleUseHistory();
    }
}
