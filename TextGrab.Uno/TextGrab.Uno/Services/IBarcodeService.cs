namespace TextGrab.Services;

/// <summary>
/// Barcode/QR code reading and generation.
/// </summary>
public interface IBarcodeService
{
    Task<string?> ReadBarcodeFromImageAsync(byte[] imageData, CancellationToken ct = default);
    Task<string?> ReadBarcodeFromStreamAsync(Stream imageStream, CancellationToken ct = default);
    byte[]? GenerateQrCode(string text, int width = 300, int height = 300);
}
