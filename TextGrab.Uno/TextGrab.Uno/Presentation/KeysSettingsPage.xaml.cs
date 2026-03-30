namespace TextGrab.Presentation;

public sealed partial class KeysSettingsPage : Page
{
    private bool _isLoading = true;

    public KeysSettingsPage()
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
            RunInBackgroundToggle.IsOn = await model.RunInTheBackground;
            GlobalHotkeysToggle.IsOn = await model.GlobalHotkeysEnabled;

#if WINDOWS
            ShortcutsPanel.Visibility = Visibility.Visible;
            HotkeyPlaceholderText.Visibility = Visibility.Collapsed;
#else
            PlatformInfoText.Visibility = Visibility.Visible;
            GlobalHotkeysToggle.IsEnabled = false;
#endif
        }
        finally
        {
            _isLoading = false;
        }
    }

    private KeysSettingsModel? GetModel() =>
        (DataContext as KeysSettingsViewModel)?.Model;

    private void RunInBackgroundToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleRunInBackground();
    }

    private void GlobalHotkeysToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleGlobalHotkeys();
    }
}
