using System.Globalization;
using TextGrab.Interfaces;

namespace TextGrab.Models;

/// <summary>
/// Cross-platform language wrapper using CultureInfo.
/// Replaces WPF's dependency on Windows.Globalization.Language.
/// </summary>
public class GlobalLang : ILanguage
{
    private readonly CultureInfo _culture;

    public GlobalLang(string languageTag)
    {
        if (languageTag == "English")
            languageTag = "en-US";

        try
        {
            _culture = new CultureInfo(languageTag);
        }
        catch (CultureNotFoundException)
        {
            _culture = CultureInfo.CurrentCulture;
        }

        LanguageTag = _culture.Name;
        AbbreviatedName = _culture.TwoLetterISOLanguageName;
        NativeName = _culture.NativeName;
        CultureDisplayName = _culture.DisplayName;
        Script = string.Empty;
        LayoutDirection = _culture.TextInfo.IsRightToLeft
            ? LanguageLayoutDirection.Rtl
            : LanguageLayoutDirection.Ltr;
    }

    public GlobalLang(CultureInfo culture)
    {
        _culture = culture;
        LanguageTag = culture.Name;
        AbbreviatedName = culture.TwoLetterISOLanguageName;
        NativeName = culture.NativeName;
        CultureDisplayName = culture.DisplayName;
        Script = string.Empty;
        LayoutDirection = culture.TextInfo.IsRightToLeft
            ? LanguageLayoutDirection.Rtl
            : LanguageLayoutDirection.Ltr;
    }

    public CultureInfo Culture => _culture;
    public string AbbreviatedName { get; }
    public string LanguageTag { get; }
    public string DisplayName => CultureDisplayName;
    public string NativeName { get; }
    public string Script { get; }
    public string CultureDisplayName { get; }
    public LanguageLayoutDirection LayoutDirection { get; }
}
