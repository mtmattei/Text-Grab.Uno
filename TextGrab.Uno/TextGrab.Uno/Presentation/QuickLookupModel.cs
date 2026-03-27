namespace TextGrab.Presentation;

public partial record QuickLookupModel
{
    private readonly INavigator _navigator;

    public QuickLookupModel(INavigator navigator)
    {
        _navigator = navigator;
    }
}
