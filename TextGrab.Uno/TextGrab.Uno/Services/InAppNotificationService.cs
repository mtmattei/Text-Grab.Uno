namespace TextGrab.Services;

/// <summary>
/// Shows in-app InfoBar notifications. Requires SetHost() to be called
/// with a Panel that can hold InfoBar controls (typically the Shell's root Grid).
/// </summary>
public class InAppNotificationService : INotificationService
{
    private Panel? _host;

    public void SetHost(Panel host) => _host = host;

    public void ShowSuccess(string message, string? title = null) =>
        Show(message, title, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);

    public void ShowInfo(string message, string? title = null) =>
        Show(message, title, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);

    public void ShowWarning(string message, string? title = null) =>
        Show(message, title, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning);

    public void ShowError(string message, string? title = null) =>
        Show(message, title, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);

    private void Show(string message, string? title, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity)
    {
        if (_host is null) return;

        var infoBar = new Microsoft.UI.Xaml.Controls.InfoBar
        {
            Title = title ?? "",
            Message = message,
            Severity = severity,
            IsOpen = true,
            IsClosable = true,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top,
            Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 0),
        };

        infoBar.Closed += (s, e) =>
        {
            _host.Children.Remove(infoBar);
        };

        _host.Children.Add(infoBar);

        // Auto-dismiss after 4 seconds
        _ = AutoDismissAsync(infoBar);
    }

    private static async Task AutoDismissAsync(Microsoft.UI.Xaml.Controls.InfoBar infoBar)
    {
        await Task.Delay(4000);
        infoBar.IsOpen = false;
    }
}
