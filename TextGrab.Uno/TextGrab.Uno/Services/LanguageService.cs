using System.Diagnostics;
using System.Globalization;

namespace TextGrab.Services;

/// <summary>
/// Cross-platform language service with caching.
/// Aggregates available languages from all registered IOcrEngine instances.
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly IEnumerable<IOcrEngine> _engines;
    private readonly IOptions<AppSettings> _settings;

    private IList<ILanguage>? _cachedAllLanguages;
    private ILanguage? _cachedCurrentInputLanguage;
    private string? _cachedCurrentInputLanguageTag;
    private string? _cachedLastUsedLang;
    private ILanguage? _cachedOcrLanguage;
    private readonly object _cacheLock = new();

    private static readonly WindowsAiLang _windowsAiLangInstance = new();

    public LanguageService(IEnumerable<IOcrEngine> engines, IOptions<AppSettings> settings)
    {
        _engines = engines;
        _settings = settings;
    }

    public ILanguage GetCurrentInputLanguage()
    {
        // Cross-platform: use CultureInfo instead of WPF InputLanguageManager
        string currentTag = CultureInfo.CurrentCulture.Name;

        lock (_cacheLock)
        {
            if (_cachedCurrentInputLanguage is not null &&
                _cachedCurrentInputLanguageTag == currentTag)
            {
                return _cachedCurrentInputLanguage;
            }

            _cachedCurrentInputLanguageTag = currentTag;
            _cachedCurrentInputLanguage = new GlobalLang(currentTag);
            return _cachedCurrentInputLanguage;
        }
    }

    public IList<ILanguage> GetAllLanguages()
    {
        lock (_cacheLock)
        {
            if (_cachedAllLanguages is not null)
                return _cachedAllLanguages;
        }

        // Run async language discovery off the UI thread to avoid deadlock
        var languages = Task.Run(async () =>
        {
            List<ILanguage> langs = [];
            foreach (IOcrEngine engine in _engines)
            {
                if (!engine.IsAvailable)
                    continue;

                try
                {
                    var engineLangs = await engine.GetAvailableLanguagesAsync().ConfigureAwait(false);
                    foreach (var lang in engineLangs)
                    {
                        if (!langs.Any(l => l.LanguageTag == lang.LanguageTag))
                            langs.Add(lang);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to get languages from {engine.Name}: {ex.Message}");
                }
            }
            return langs;
        }).GetAwaiter().GetResult();

        lock (_cacheLock)
        {
            _cachedAllLanguages ??= languages;
            return _cachedAllLanguages;
        }
    }

    public ILanguage GetOcrLanguage()
    {
        string lastUsedLang = _settings.Value.LastUsedLang;

        lock (_cacheLock)
        {
            if (_cachedOcrLanguage is not null && _cachedLastUsedLang == lastUsedLang)
                return _cachedOcrLanguage;

            _cachedLastUsedLang = lastUsedLang;
            ILanguage selectedLanguage = GetCurrentInputLanguage();

            if (!string.IsNullOrEmpty(lastUsedLang))
            {
                if (lastUsedLang == _windowsAiLangInstance.LanguageTag)
                {
                    _cachedOcrLanguage = _windowsAiLangInstance;
                    return _cachedOcrLanguage;
                }

                try
                {
                    selectedLanguage = new GlobalLang(lastUsedLang);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to parse LastUsedLang: {lastUsedLang}\n{ex.Message}");
                    selectedLanguage = GetCurrentInputLanguage();
                }
            }

            IList<ILanguage> possibleLanguages = GetAllLanguages();

            if (possibleLanguages.Count == 0)
            {
                _cachedOcrLanguage = new GlobalLang("en-US");
                return _cachedOcrLanguage;
            }

            // Check if selected language is available
            if (possibleLanguages.All(l => l.LanguageTag != selectedLanguage.LanguageTag))
            {
                var similar = possibleLanguages.Where(
                    la => la.LanguageTag.Contains(selectedLanguage.LanguageTag)
                    || selectedLanguage.LanguageTag.Contains(la.LanguageTag)
                ).ToList();

                _cachedOcrLanguage = similar.Count > 0
                    ? new GlobalLang(similar.First().LanguageTag)
                    : new GlobalLang(possibleLanguages.First().LanguageTag);

                return _cachedOcrLanguage;
            }

            _cachedOcrLanguage = selectedLanguage;
            return _cachedOcrLanguage;
        }
    }

}
