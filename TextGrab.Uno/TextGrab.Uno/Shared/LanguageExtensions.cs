using System.Globalization;
using TextGrab.Interfaces;

namespace TextGrab;

public static class LanguageExtensions
{
    public static bool IsSpaceJoining(this ILanguage selectedLanguage)
    {
        if (selectedLanguage.LanguageTag.StartsWith("zh", StringComparison.InvariantCultureIgnoreCase))
            return false;
        else if (selectedLanguage.LanguageTag.Equals("ja", StringComparison.InvariantCultureIgnoreCase))
            return false;
        return true;
    }

    public static bool IsRightToLeft(this ILanguage selectedLanguage)
    {
        return selectedLanguage.LayoutDirection == LanguageLayoutDirection.Rtl;
    }

    public static bool IsLatinBased(this ILanguage selectedLanguage)
    {
        string[] latinLanguages = ["en", "es", "fr", "it", "ro", "pt"];
        string tag = selectedLanguage.LanguageTag;
        return latinLanguages.Any(lang => tag.StartsWith(lang, StringComparison.InvariantCultureIgnoreCase));
    }

    public static LanguageKind GetLanguageKind(this ILanguage language) => language switch
    {
        Models.WindowsAiLang => LanguageKind.WindowsAi,
        Models.TessLang => LanguageKind.Tesseract,
        _ => LanguageKind.Global,
    };
}
