namespace TextGrab.Presentation;

public partial record FullscreenGrabModel
{
    private readonly INavigator _navigator;
    private readonly IOptions<AppSettings> _settings;

    public FullscreenGrabModel(
        INavigator navigator,
        IOptions<AppSettings> settings)
    {
        _navigator = navigator;
        _settings = settings;
    }

    public IState<string> OcrResultText => State<string>.Value(this, () => "");
    public IState<bool> IsBusy => State<bool>.Value(this, () => false);
    public IState<string> StatusText => State<string>.Value(this, () => "Draw a rectangle to capture text");
}
