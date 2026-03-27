namespace TextGrab.Presentation;

public partial record QuickLookupModel
{
    private readonly INavigator _navigator;
    private readonly IFileService _fileService;
    private readonly IOptions<AppSettings> _settings;

    public QuickLookupModel(
        INavigator navigator,
        IFileService fileService,
        IOptions<AppSettings> settings)
    {
        _navigator = navigator;
        _fileService = fileService;
        _settings = settings;
    }

    // --- State ---

    public IState<string> SearchText => State<string>.Value(this, () => string.Empty);
    public IState<bool> IsRegexSearch => State<bool>.Value(this, () => false);
    public IState<string> StatusText => State<string>.Value(this, () => "Load a CSV or paste data");
    public IState<bool> HasUnsavedChanges => State<bool>.Value(this, () => false);

    // --- Commands ---

    public async ValueTask ToggleRegex()
    {
        var current = await IsRegexSearch;
        await IsRegexSearch.Set(!current, CancellationToken.None);
    }
}
