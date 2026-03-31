namespace TextGrab.Services;

/// <summary>
/// Captures screen regions as image streams for OCR processing.
/// Windows: Uses Windows.Graphics.Capture or GDI+.
/// Other platforms: Not supported (use file picker fallback).
/// </summary>
public interface IScreenCaptureService
{
    bool IsSupported { get; }
    Task<Stream?> CaptureScreenAsync(CancellationToken ct = default);
    Task<Stream?> CaptureRegionAsync(Windows.Foundation.Rect region, CancellationToken ct = default);
}
