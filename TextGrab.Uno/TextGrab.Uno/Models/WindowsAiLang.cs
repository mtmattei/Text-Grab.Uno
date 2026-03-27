using TextGrab.Interfaces;

namespace TextGrab.Models;

/// <summary>
/// Sentinel language type representing Windows AI OCR engine selection.
/// Only meaningful on Windows 11 ARM64+ devices with AI features.
/// </summary>
public class WindowsAiLang : ILanguage
{
    public string AbbreviatedName => "WinAI";
    public string DisplayName => "Windows AI OCR";
    public string CultureDisplayName => "Windows AI OCR";
    public string LanguageTag => "WinAI";
    public LanguageLayoutDirection LayoutDirection => LanguageLayoutDirection.Ltr;
    public string NativeName => "Windows AI OCR";
    public string Script => string.Empty;
}
