using System.Diagnostics;

namespace TextGrab.Services;

/// <summary>
/// Facade that routes OCR requests to the appropriate engine based on language type.
/// Applies user settings for error correction and output formatting.
/// </summary>
public class OcrService : IOcrService
{
    private readonly IEnumerable<IOcrEngine> _engines;
    private readonly ILanguageService _languageService;
    private readonly IOptions<AppSettings> _settings;

    public OcrService(
        IEnumerable<IOcrEngine> engines,
        ILanguageService languageService,
        IOptions<AppSettings> settings)
    {
        _engines = engines;
        _languageService = languageService;
        _settings = settings;
    }

    public IReadOnlyList<IOcrEngine> Engines => _engines.ToList().AsReadOnly();

    public async Task<OcrOutput?> RecognizeAsync(Stream imageStream, ILanguage? language = null, CancellationToken ct = default)
    {
        language ??= _languageService.GetOcrLanguage();

        // Select engine based on language type
        IOcrEngine? engine = SelectEngine(language);
        if (engine is null)
        {
            Debug.WriteLine("No available OCR engine found");
            return null;
        }

        // Run OCR
        IOcrLinesWords? result = await engine.RecognizeAsync(imageStream, language, ct);
        if (result is null)
            return null;

        // Format output
        var settings = _settings.Value;
        OcrOutput output = OcrUtilities.GetTextFromOcrResult(
            result,
            language,
            settings.CorrectErrors,
            settings.CorrectToLatin);

        output = output with { Engine = engine.Kind };

        // Apply post-processing
        output.CleanOutput(settings.CorrectToLatin, settings.CorrectErrors);

        return output;
    }

    private IOcrEngine? SelectEngine(ILanguage language)
    {
        var kind = language.GetLanguageKind();

        // Try to find an engine matching the language kind
        IOcrEngine? engine = kind switch
        {
            LanguageKind.Tesseract => _engines.FirstOrDefault(e => e.Kind == OcrEngineKind.Tesseract && e.IsAvailable),
            LanguageKind.WindowsAi => _engines.FirstOrDefault(e => e.Name == "Windows AI" && e.IsAvailable),
            _ => _engines.FirstOrDefault(e => e.Kind == OcrEngineKind.Windows && e.IsAvailable),
        };

        // Fallback to any available engine
        return engine ?? _engines.FirstOrDefault(e => e.IsAvailable);
    }
}
