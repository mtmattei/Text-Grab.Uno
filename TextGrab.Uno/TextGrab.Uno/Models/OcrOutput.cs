namespace TextGrab.Models;

/// <summary>
/// High-level OCR result combining engine metadata with text output.
/// CleanOutput() applies user-configured error correction.
/// </summary>
public record OcrOutput
{
    public OcrEngineKind Engine { get; init; } = OcrEngineKind.Windows;
    public OcrOutputKind Kind { get; init; } = OcrOutputKind.None;
    public string RawOutput { get; init; } = string.Empty;
    public string CleanedOutput { get; set; } = string.Empty;
    public string LanguageTag { get; init; } = string.Empty;

    /// <summary>
    /// Structured OCR result with line/word bounding boxes.
    /// Null when the result is text-only (e.g., Tesseract plain text mode).
    /// </summary>
    public IOcrLinesWords? StructuredResult { get; init; }

    /// <summary>
    /// Applies error correction based on user settings.
    /// </summary>
    /// <summary>
    /// Returns the best available text — CleanedOutput if available, otherwise RawOutput.
    /// </summary>
    public string GetBestText() =>
        !string.IsNullOrWhiteSpace(CleanedOutput) ? CleanedOutput : RawOutput;

    /// <summary>
    /// Applies error correction based on user settings.
    /// </summary>
    public void CleanOutput(bool correctToLatin, bool correctErrors)
    {
        if (Kind == OcrOutputKind.Barcode)
            return;

        string correcting = RawOutput;

        if (correctToLatin)
            correcting = correcting.ReplaceGreekOrCyrillicWithLatin();

        if (correctErrors)
            correcting = correcting.TryFixEveryWordLetterNumberErrors();

        CleanedOutput = correcting;
    }
}
