#if WINDOWS
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace TextGrab.Services;

/// <summary>
/// IOcrEngine implementation wrapping Windows.Media.Ocr (WinRT OCR).
/// Available on all Windows 10/11 devices.
/// </summary>
public class WindowsOcrEngine : IOcrEngine
{
    public string Name => "Windows OCR";
    public OcrEngineKind Kind => OcrEngineKind.Windows;
    public bool IsAvailable => true;

    public async Task<IOcrLinesWords?> RecognizeAsync(Stream imageStream, ILanguage language, CancellationToken ct = default)
    {
        // Decode stream to SoftwareBitmap
        var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        // Create WinRT OCR engine for the requested language
        var winLang = new Windows.Globalization.Language(language.LanguageTag);
        var engine = OcrEngine.TryCreateFromLanguage(winLang);

        if (engine is null)
        {
            // Fallback to current culture
            var fallbackLang = new Windows.Globalization.Language(System.Globalization.CultureInfo.CurrentCulture.Name);
            engine = OcrEngine.TryCreateFromLanguage(fallbackLang)
                  ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));
        }

        if (engine is null)
            return null;

        var ocrResult = await engine.RecognizeAsync(softwareBitmap);
        return new WinRtOcrLinesWords(ocrResult);
    }

    public Task<IReadOnlyList<ILanguage>> GetAvailableLanguagesAsync(CancellationToken ct = default)
    {
        var languages = OcrEngine.AvailableRecognizerLanguages
            .Select(lang => (ILanguage)new GlobalLang(lang.LanguageTag))
            .ToList();

        return Task.FromResult<IReadOnlyList<ILanguage>>(languages);
    }
}
#endif
