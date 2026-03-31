namespace TextGrab.Presentation;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
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

        // Check first run — show dialog
        if (settings?.Value?.FirstRun != false)
        {
            _ = ShowFirstRunDialogAsync();
        }
    }

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

        // Mark first run complete
        var writableSettings = ((App)Application.Current).Host?.Services
            .GetService<global::Uno.Extensions.Configuration.IWritableOptions<AppSettings>>();
        if (writableSettings is not null)
            await writableSettings.UpdateAsync(s => s with { FirstRun = false });
    }
}
