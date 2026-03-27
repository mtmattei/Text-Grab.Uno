using Windows.Foundation;

namespace TextGrab.Models;

/// <summary>
/// Platform-agnostic OCR result with structured lines, words, and bounding boxes.
/// Implemented by WinRtOcrLinesWords, WinAiOcrLinesWords, and TesseractOcrLinesWords.
/// </summary>
public interface IOcrLinesWords
{
    string Text { get; set; }
    IOcrLine[] Lines { get; set; }
    float Angle { get; set; }
}

public interface IOcrLine
{
    string Text { get; set; }
    IOcrWord[] Words { get; set; }
    Rect BoundingBox { get; set; }
}

public interface IOcrWord
{
    string Text { get; set; }
    Rect BoundingBox { get; set; }
}

/// <summary>
/// Simple implementation for engines that produce structured results
/// without a platform-specific backing type (e.g., Tesseract hOCR).
/// </summary>
public class SimpleOcrLinesWords : IOcrLinesWords
{
    public string Text { get; set; } = string.Empty;
    public IOcrLine[] Lines { get; set; } = [];
    public float Angle { get; set; }
}

public class SimpleOcrLine : IOcrLine
{
    public string Text { get; set; } = string.Empty;
    public IOcrWord[] Words { get; set; } = [];
    public Rect BoundingBox { get; set; }
}

public class SimpleOcrWord : IOcrWord
{
    public string Text { get; set; } = string.Empty;
    public Rect BoundingBox { get; set; }
}
