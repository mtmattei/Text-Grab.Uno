namespace TextGrab.Presentation;

public partial record GrabFrameModel
{
    private readonly INavigator _navigator;
    private readonly IOcrService _ocrService;
    private readonly ILanguageService _languageService;
    private readonly IFileService _fileService;
    private readonly IOptions<AppSettings> _settings;

    public GrabFrameModel(
        INavigator navigator,
        IOcrService ocrService,
        ILanguageService languageService,
        IFileService fileService,
        IOptions<AppSettings> settings)
    {
        _navigator = navigator;
        _ocrService = ocrService;
        _languageService = languageService;
        _fileService = fileService;
        _settings = settings;
    }

    // --- State ---

    public IState<string> FrameText => State<string>.Value(this, () => string.Empty);
    public IState<string> StatusText => State<string>.Value(this, () => "Load an image to start OCR");
    public IState<bool> IsTableMode => State<bool>.Value(this, () => false);
    public IState<bool> IsEditMode => State<bool>.Value(this, () => true);
    public IState<bool> IsOcrBusy => State<bool>.Value(this, () => false);
    public IState<int> MatchCount => State<int>.Value(this, () => 0);

    // Language selection
    public IListFeed<ILanguage> AvailableLanguages => ListFeed.Async(async ct =>
    {
        var languages = _languageService.GetAllLanguages();
        return languages.ToImmutableList();
    });

    public IState<ILanguage> SelectedLanguage => State<ILanguage>.Value(this, () => _languageService.GetOcrLanguage());

    // --- Commands ---

    public async ValueTask ToggleTableMode()
    {
        var current = await IsTableMode;
        await IsTableMode.Set(!current, CancellationToken.None);
    }

    public async ValueTask ToggleEditMode()
    {
        var current = await IsEditMode;
        await IsEditMode.Set(!current, CancellationToken.None);
    }
}
