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
    private string? _cachedSystemLanguageForTranslation;
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

            List<ILanguage> languages = [];

            // Aggregate languages from all registered engines
            foreach (IOcrEngine engine in _engines)
            {
                if (!engine.IsAvailable)
                    continue;

                try
                {
                    var engineLangs = engine.GetAvailableLanguagesAsync().GetAwaiter().GetResult();
                    foreach (var lang in engineLangs)
                    {
                        // Avoid duplicates by tag
                        if (!languages.Any(l => l.LanguageTag == lang.LanguageTag))
                            languages.Add(lang);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to get languages from {engine.Name}: {ex.Message}");
                }
            }

            _cachedAllLanguages = languages;
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

    public string GetSystemLanguageForTranslation()
    {
        string currentTag = CultureInfo.CurrentCulture.Name;

        lock (_cacheLock)
        {
            if (_cachedSystemLanguageForTranslation is not null &&
                _cachedCurrentInputLanguageTag == currentTag)
            {
                return _cachedSystemLanguageForTranslation;
            }

            try
            {
                ILanguage currentLang = GetCurrentInputLanguage();
                string displayName = currentLang.DisplayName;

                if (displayName.Contains('('))
                    displayName = displayName[..displayName.IndexOf('(')].Trim();

                string languageTag = currentLang.LanguageTag.ToLowerInvariant();
                _cachedSystemLanguageForTranslation = languageTag switch
                {
                    var t when t.StartsWith("en") => "English",
                    var t when t.StartsWith("es") => "Spanish",
                    var t when t.StartsWith("fr") => "French",
                    var t when t.StartsWith("de") => "German",
                    var t when t.StartsWith("it") => "Italian",
                    var t when t.StartsWith("pt") => "Portuguese",
                    var t when t.StartsWith("ru") => "Russian",
                    var t when t.StartsWith("ja") => "Japanese",
                    var t when t.StartsWith("zh") => "Chinese",
                    var t when t.StartsWith("ko") => "Korean",
                    var t when t.StartsWith("ar") => "Arabic",
                    var t when t.StartsWith("hi") => "Hindi",
                    _ => displayName
                };

                return _cachedSystemLanguageForTranslation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get system language: {ex.Message}");
                _cachedSystemLanguageForTranslation = "English";
                return _cachedSystemLanguageForTranslation;
            }
        }
    }

    public bool IsCurrentLanguageLatinBased() => GetCurrentInputLanguage().IsLatinBased();

    public void InvalidateLanguagesCache()
    {
        lock (_cacheLock)
        {
            _cachedAllLanguages = null;
            _cachedOcrLanguage = null;
        }
    }

    public void InvalidateOcrLanguageCache()
    {
        lock (_cacheLock)
        {
            _cachedOcrLanguage = null;
            _cachedLastUsedLang = null;
        }
    }

    public void InvalidateAllCaches()
    {
        lock (_cacheLock)
        {
            _cachedAllLanguages = null;
            _cachedCurrentInputLanguage = null;
            _cachedCurrentInputLanguageTag = null;
            _cachedSystemLanguageForTranslation = null;
            _cachedLastUsedLang = null;
            _cachedOcrLanguage = null;
        }
    }
}
