namespace TextGrab.Interfaces;

/// <summary>
/// Abstraction for OCR language across Windows Runtime, Tesseract, and Windows AI engines.
/// Replaces dependency on Windows.Globalization.Language.
/// </summary>
public interface ILanguage
{
    string AbbreviatedName { get; }
    string LanguageTag { get; }
    string DisplayName { get; }
    string NativeName { get; }
    string Script { get; }
    string CultureDisplayName { get; }
    LanguageLayoutDirection LayoutDirection { get; }

    static bool IsWellFormed(string languageTag) => true;
}
