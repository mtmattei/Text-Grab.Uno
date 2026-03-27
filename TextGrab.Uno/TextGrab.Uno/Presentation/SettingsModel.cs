namespace TextGrab.Presentation;

public partial record SettingsModel
{
    private readonly INavigator _navigator;

    public SettingsModel(INavigator navigator)
    {
        _navigator = navigator;
    }
}
