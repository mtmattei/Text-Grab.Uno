namespace TextGrab.Presentation;

public sealed partial class FirstRunPage : Page
{
    private bool _isLoading = true;

    public FirstRunPage()
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
            // Set default launch radio button
            var launch = await model.DefaultLaunch ?? "EditText";
            switch (launch)
            {
                case "Fullscreen": FullScreenRDBTN.IsChecked = true; break;
                case "GrabFrame": GrabFrameRDBTN.IsChecked = true; break;
                case "QuickLookup": QuickLookupRDBTN.IsChecked = true; break;
                default: EditWindowRDBTN.IsChecked = true; break;
            }

            NotificationsToggle.IsOn = await model.ShowToast;
            BackgroundToggle.IsOn = await model.RunInTheBackground;

#if WINDOWS
            StartupToggle.Visibility = Visibility.Visible;
            StartupToggle.IsOn = await model.StartupOnLogin;
#endif
        }
        finally
        {
            _isLoading = false;
        }
    }

    private FirstRunModel? GetModel() =>
        (DataContext as FirstRunViewModel)?.Model;

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;

        string launch;
        if (FullScreenRDBTN.IsChecked == true) launch = "Fullscreen";
        else if (GrabFrameRDBTN.IsChecked == true) launch = "GrabFrame";
        else if (QuickLookupRDBTN.IsChecked == true) launch = "QuickLookup";
        else launch = "EditText";

        _ = model.SetDefaultLaunch(launch);
    }

    private void NotificationsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleShowToast();
    }

    private void BackgroundToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleRunInBackground();
    }

    private void StartupToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleStartupOnLogin();
    }

    private void TryFullscreen_Click(object sender, RoutedEventArgs e)
    {
        // TODO Phase 7: FullscreenGrab not yet ported — navigate to GrabFrame as fallback
        if (GetModel() is { } model)
            _ = model.NavigateToDefaultPage();
    }

    private void TryGrabFrame_Click(object sender, RoutedEventArgs e)
    {
        if (GetModel() is { } model)
        {
            _ = model.CompleteFirstRun();
            // Navigate via Shell
        }
    }

    private void TryEditWindow_Click(object sender, RoutedEventArgs e)
    {
        if (GetModel() is { } model)
        {
            _ = model.CompleteFirstRun();
        }
    }

    private void TryQuickLookup_Click(object sender, RoutedEventArgs e)
    {
        if (GetModel() is { } model)
        {
            _ = model.CompleteFirstRun();
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetModel() is { } model)
            _ = model.NavigateToSettings();
    }

    private void OkayButton_Click(object sender, RoutedEventArgs e)
    {
        if (GetModel() is { } model)
            _ = model.NavigateToDefaultPage();
    }
}
