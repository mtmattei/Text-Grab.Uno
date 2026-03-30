namespace TextGrab.Services;

public interface INotificationService
{
    void ShowSuccess(string message, string? title = null);
    void ShowInfo(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowError(string message, string? title = null);
}
