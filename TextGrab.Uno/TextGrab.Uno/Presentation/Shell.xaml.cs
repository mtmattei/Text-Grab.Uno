namespace TextGrab.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    public Shell()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    public ContentControl ContentControl => Splash;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Apply saved theme on startup
        var settings = this.GetService<IOptions<AppSettings>>();
        var theme = settings?.Value?.AppTheme ?? "System";

        if (this.XamlRoot is not null && theme != "System")
        {
            var elementTheme = theme switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
            global::Uno.Toolkit.UI.SystemThemeHelper.SetApplicationTheme(this.XamlRoot, elementTheme);
        }
    }
}
