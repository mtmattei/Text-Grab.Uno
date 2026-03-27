namespace TextGrab.Services;

/// <summary>
/// Abstraction for OCR engines (Windows Runtime, Tesseract, Windows AI).
/// Each engine returns structured results with bounding boxes.
/// </summary>
public interface IOcrEngine
{
    string Name { get; }
    OcrEngineKind Kind { get; }
    bool IsAvailable { get; }

    /// <summary>
    /// Runs OCR on an image stream, returning structured lines/words with bounding boxes.
    /// </summary>
    Task<IOcrLinesWords?> RecognizeAsync(Stream imageStream, Interfaces.ILanguage language, CancellationToken ct = default);

    /// <summary>
    /// Returns the languages this engine supports.
    /// </summary>
    Task<IReadOnlyList<Interfaces.ILanguage>> GetAvailableLanguagesAsync(CancellationToken ct = default);
}
