using Uno.Extensions.Configuration;

namespace TextGrab.Presentation;

public partial record LanguageSettingsModel
{
    private readonly IWritableOptions<AppSettings> _settings;
    private readonly ILanguageService _languageService;

    public LanguageSettingsModel(
        IWritableOptions<AppSettings> settings,
        ILanguageService languageService)
    {
        _settings = settings;
        _languageService = languageService;
    }

    public IState<bool> UseTesseract => State<bool>.Value(this, () => _settings.Value?.UseTesseract ?? false);
    public IState<string> TesseractPath => State<string>.Value(this, () => _settings.Value?.TesseractPath ?? "");

    public IListFeed<string> InstalledLanguages => ListFeed.Async(ct =>
    {
        var langs = _languageService.GetAllLanguages();
        return ValueTask.FromResult(langs.Select(l => l.DisplayName).ToImmutableList());
    });

    public async ValueTask ToggleTesseract()
    {
        var current = await UseTesseract;
        await UseTesseract.Set(!current, CancellationToken.None);
        await _settings.UpdateAsync(s => s with { UseTesseract = !current });
    }
}
