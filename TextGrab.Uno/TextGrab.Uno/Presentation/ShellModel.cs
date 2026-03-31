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

    }
}
