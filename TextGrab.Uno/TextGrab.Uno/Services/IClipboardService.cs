namespace TextGrab.Services;

/// <summary>
/// Cross-platform clipboard operations.
/// </summary>
public interface IClipboardService
{
    Task<string?> GetTextAsync(CancellationToken ct = default);
    Task SetTextAsync(string text, CancellationToken ct = default);
    Task<byte[]?> GetImageAsync(CancellationToken ct = default);
}
