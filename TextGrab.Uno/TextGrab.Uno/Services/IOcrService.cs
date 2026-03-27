namespace TextGrab.Services;

/// <summary>
/// High-level OCR service that orchestrates engine selection, image scaling, and text extraction.
/// This is the main entry point for OCR operations from the UI layer.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Runs OCR on an image stream using the best available engine for the given language.
    /// Returns structured results with bounding boxes.
    /// </summary>
    Task<OcrOutput?> RecognizeAsync(Stream imageStream, ILanguage? language = null, CancellationToken ct = default);

    /// <summary>
    /// Gets all available engines.
    /// </summary>
    IReadOnlyList<IOcrEngine> Engines { get; }
}
