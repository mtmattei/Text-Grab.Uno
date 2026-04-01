using SkiaSharp;
using ZXing;

namespace TextGrab.Services;

/// <summary>
/// Cross-platform barcode/QR code reading and generation using ZXing.Net + SkiaSharp.
/// </summary>
public class BarcodeService : IBarcodeService
{
    public Task<string?> ReadBarcodeFromImageAsync(byte[] imageData, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var bitmap = SKBitmap.Decode(imageData);
                if (bitmap is null) return null;

                // Convert SKBitmap pixels to ZXing-compatible format
                var pixels = bitmap.Pixels;
                var luminanceSource = new RGBLuminanceSource(
                    GetRgbBytes(pixels),
                    bitmap.Width,
                    bitmap.Height);

                var reader = new BarcodeReaderGeneric
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions { TryHarder = true }
                };

                var result = reader.Decode(luminanceSource);
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }, ct);
    }

    public Task<string?> ReadBarcodeFromStreamAsync(Stream imageStream, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var bitmap = SKBitmap.Decode(imageStream);
                if (bitmap is null) return null;

                var pixels = bitmap.Pixels;
                var luminanceSource = new RGBLuminanceSource(
                    GetRgbBytes(pixels),
                    bitmap.Width,
                    bitmap.Height);

                var reader = new BarcodeReaderGeneric
                {
                    AutoRotate = true,
                    Options = new ZXing.Common.DecodingOptions { TryHarder = true }
                };

                var result = reader.Decode(luminanceSource);
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }, ct);
    }

    public byte[]? GenerateQrCode(string text, int width = 300, int height = 300)
    {
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 2
                }
            };

            var pixelData = writer.Write(text);

            // Convert pixel data to PNG via SkiaSharp
            using var bitmap = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private static byte[] GetRgbBytes(SKColor[] pixels)
    {
        var bytes = new byte[pixels.Length * 3];
        for (int i = 0; i < pixels.Length; i++)
        {
            bytes[i * 3] = pixels[i].Red;
            bytes[i * 3 + 1] = pixels[i].Green;
            bytes[i * 3 + 2] = pixels[i].Blue;
        }
        return bytes;
    }
}
