using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public sealed partial class FirstRunPage : Page
{
    private bool _isLoading = true;

    public event EventHandler? FirstRunCompleted;

    public FirstRunPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        try
        {
            var settings = ((App)Application.Current).Host?.Services.GetService<IOptions<AppSettings>>();
            var launch = settings?.Value?.DefaultLaunch ?? "EditText";
            switch (launch)
            {
                case "Fullscreen": FullScreenRDBTN.IsChecked = true; break;
                case "GrabFrame": GrabFrameRDBTN.IsChecked = true; break;
                case "QuickLookup": QuickLookupRDBTN.IsChecked = true; break;
                default: EditWindowRDBTN.IsChecked = true; break;
            }

            NotificationsToggle.IsOn = settings?.Value?.ShowToast ?? true;
            BackgroundToggle.IsOn = settings?.Value?.RunInTheBackground ?? false;

#if WINDOWS
            StartupToggle.Visibility = Visibility.Visible;
            StartupToggle.IsOn = settings?.Value?.StartupOnLogin ?? false;
#endif
        }
        finally
        {
            _isLoading = false;
        }
    }

    private IWritableOptions<AppSettings>? GetWritableSettings() =>
        ((App)Application.Current).Host?.Services.GetService<IWritableOptions<AppSettings>>();

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        string launch;
        if (FullScreenRDBTN.IsChecked == true) launch = "Fullscreen";
        else if (GrabFrameRDBTN.IsChecked == true) launch = "GrabFrame";
        else if (QuickLookupRDBTN.IsChecked == true) launch = "QuickLookup";
        else launch = "EditText";

        _ = GetWritableSettings()?.UpdateAsync(s => s with { DefaultLaunch = launch });
    }

    private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _ = GetWritableSettings()?.UpdateAsync(s => s with { ShowToast = NotificationsToggle.IsOn });
    }

    private void BackgroundToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _ = GetWritableSettings()?.UpdateAsync(s => s with { RunInTheBackground = BackgroundToggle.IsOn });
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _ = GetWritableSettings()?.UpdateAsync(s => s with { StartupOnLogin = StartupToggle.IsOn });
    }

    private async void CompleteAndNavigateToShell(object sender, RoutedEventArgs e)
    {
        // Mark first run done
        var ws = GetWritableSettings();
        if (ws is not null)
            await ws.UpdateAsync(s => s with { FirstRun = false });

        // Navigate to Shell (sibling route, clear back stack)
        var navigator = ((App)Application.Current).Host?.Services.GetRequiredService<INavigator>();
        if (navigator is not null)
            await navigator.NavigateRouteAsync(this, "Shell");
    }

    private void TryFullscreen_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
    private void TryGrabFrame_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
    private void TryEditWindow_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
    private void TryQuickLookup_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
    private void SettingsButton_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
    private void OkayButton_Click(object sender, RoutedEventArgs e) => CompleteAndNavigateToShell(sender, e);
}
