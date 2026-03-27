namespace TextGrab.Services;

/// <summary>
/// Provides cached access to OCR language information.
/// Replaces WPF's static LanguageUtilities facade.
/// </summary>
public interface ILanguageService
{
    ILanguage GetCurrentInputLanguage();
    IList<ILanguage> GetAllLanguages();
    ILanguage GetOcrLanguage();
    string GetSystemLanguageForTranslation();
    bool IsCurrentLanguageLatinBased();
    void InvalidateLanguagesCache();
    void InvalidateOcrLanguageCache();
    void InvalidateAllCaches();
}
