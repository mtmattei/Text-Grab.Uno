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
        if (notificationService is not null)
        {
            NotificationHost.IsHitTestVisible = true;
            notificationService.SetHost(NotificationHost);
        }
    }
}
