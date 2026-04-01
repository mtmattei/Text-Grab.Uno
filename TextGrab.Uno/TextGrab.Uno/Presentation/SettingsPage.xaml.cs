namespace TextGrab.Presentation;

public sealed partial class SettingsPage : Page
{
    private static readonly Dictionary<string, Type> SettingsPageMap = new()
    {
        ["GeneralSettings"] = typeof(GeneralSettingsPage),
        ["FullscreenGrabSettings"] = typeof(FullscreenGrabSettingsPage),
        ["LanguageSettings"] = typeof(LanguageSettingsPage),
        ["KeysSettings"] = typeof(KeysSettingsPage),
        ["TesseractSettings"] = typeof(TesseractSettingsPage),
        ["DangerSettings"] = typeof(DangerSettingsPage),
    };

    public SettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (SettingsNavView.MenuItems.Count > 0)
            SettingsNavView.SelectedItem = SettingsNavView.MenuItems[0];
    }

    private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            if (SettingsPageMap.TryGetValue(tag, out var pageType))
                SettingsFrame.Navigate(pageType);
        }
    }
}
