namespace TextGrab.Presentation;

public class ShellModel
{
    private readonly INavigator _navigator;
    private readonly IOptions<AppSettings> _settings;

    public ShellModel(
        INavigator navigator,
        IOptions<AppSettings> settings)
    {
        _navigator = navigator;
        _settings = settings;

        CheckFirstRun();
    }

    private async void CheckFirstRun()
    {
        if (_settings.Value?.FirstRun == true)
        {
            await _navigator.NavigateRouteAsync(this, "FirstRun");
        }
    }
}
