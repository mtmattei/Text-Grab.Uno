namespace TextGrab.Services;

public interface ISystemTrayService
{
    bool IsSupported { get; }
    void ShowTrayIcon(string tooltip = "Text Grab");
    void HideTrayIcon();
    event EventHandler? ShowWindowRequested;
    event EventHandler? ExitRequested;
}
