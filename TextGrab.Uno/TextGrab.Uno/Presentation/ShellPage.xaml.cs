namespace TextGrab.Presentation;

public sealed partial class ShellPage : Page
{
    private static readonly Dictionary<string, Type> PageMap = new()
    {
        ["EditText"] = typeof(EditTextPage),
        ["GrabFrame"] = typeof(GrabFramePage),
        ["QuickLookup"] = typeof(QuickLookupPage),
        ["Settings"] = typeof(SettingsPage),
        ["FullscreenGrab"] = typeof(FullscreenGrabPage),
    };

    public ShellPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    public void NavigateTo(string tag)
    {
        if (PageMap.TryGetValue(tag, out var pageType))
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Wire notification host
        var notificationService = ((App)Application.Current).Host?.Services
            .GetService<InAppNotificationService>();
        notificationService?.SetHost(NotificationHost);

        // Apply saved theme
        var settings = ((App)Application.Current).Host?.Services.GetService<IOptions<AppSettings>>();
        var theme = settings?.Value?.AppTheme;
        if (this.XamlRoot is not null && theme is not null && theme != "System")
        {
            var elementTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
            global::Uno.Toolkit.UI.SystemThemeHelper.SetApplicationTheme(this.XamlRoot, elementTheme);
        }

#if WINDOWS
        if (settings?.Value?.RunInTheBackground == true)
            WireBackgroundMode();
#endif

        if (settings?.Value?.FirstRun != false)
            _ = ShowFirstRunDialogAsync();

        // Navigate to default page
        if (NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

#if WINDOWS
    private void WireBackgroundMode()
    {
        if (App.MainWindow is null) return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        var trayService = ((App)Application.Current).Host?.Services
            .GetService<WindowsSystemTrayService>();
        if (trayService is not null)
        {
            trayService.SetWindowHandle(hwnd);
            trayService.ShowTrayIcon("Text Grab — Running in background");
            trayService.ShowWindowRequested += (s, e) =>
            {
                ShowWindow(hwnd, 9);
                SetForegroundWindow(hwnd);
            };
        }

        if (appWindow is not null)
        {
            appWindow.Closing += (s, args) =>
            {
                var currentSettings = ((App)Application.Current).Host?.Services.GetService<IOptions<AppSettings>>();
                if (currentSettings?.Value?.RunInTheBackground == true)
                {
                    args.Cancel = true;
                    ShowWindow(hwnd, 6);
                }
            };
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);
#endif

    private async Task ShowFirstRunDialogAsync()
    {
        if (this.XamlRoot is null) return;

        var dialog = new ContentDialog
        {
            Title = "Welcome to Text Grab!",
            Content = "Text Grab has four modes:\n\n" +
                      "1. Full-Screen - Like a screenshot, but for copying text\n" +
                      "2. Grab Frame - An overlay for picking and finding text\n" +
                      "3. Edit Text - Like Notepad, with tools for fixing text\n" +
                      "4. Quick Lookup - A searchable list for quick copy\n\n" +
                      "Use the sidebar to switch between modes. Visit Settings to customize.",
            PrimaryButtonText = "Get Started",
            XamlRoot = this.XamlRoot,
        };

        await dialog.ShowAsync();

        var writableSettings = ((App)Application.Current).Host?.Services
            .GetService<global::Uno.Extensions.Configuration.IWritableOptions<AppSettings>>();
        if (writableSettings is not null)
            await writableSettings.UpdateAsync(s => s with { FirstRun = false });
    }
}
