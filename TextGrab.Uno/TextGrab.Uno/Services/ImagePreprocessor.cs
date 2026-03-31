using SkiaSharp;

namespace TextGrab.Services;

/// <summary>
/// Cross-platform image preprocessing using SkiaSharp.
/// Replaces WPF's Magick.NET and GDI+ for OCR optimization.
/// </summary>
public static class ImagePreprocessor
{
    /// <summary>
    /// Preprocesses an image stream for better OCR accuracy.
    /// Applies grayscale conversion, contrast enhancement, and optional scaling.
    /// </summary>
    public static Stream Preprocess(Stream input, double scaleFactor = 1.0, bool grayscale = true, bool enhanceContrast = true)
    {
        using var original = SKBitmap.Decode(input);
        if (original is null)
            return input;

        // Scale if needed
        SKBitmap working;
        if (scaleFactor != 1.0 && scaleFactor > 0)
        {
            int newWidth = (int)(original.Width * scaleFactor);
            int newHeight = (int)(original.Height * scaleFactor);
            working = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        }
        else
        {
            working = original.Copy();
        }

        // Apply grayscale
        if (grayscale)
        {
            ApplyGrayscale(working);
        }

        // Enhance contrast
        if (enhanceContrast)
        {
            ApplyContrastStretch(working);
        }

        // Encode to PNG stream
        var output = new MemoryStream();
        using var image = SKImage.FromBitmap(working);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(output);
        output.Position = 0;

        working.Dispose();
        return output;
    }

    /// <summary>
    /// Converts image to grayscale in-place.
    /// </summary>
    private static void ApplyGrayscale(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                byte gray = (byte)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);
                bitmap.SetPixel(x, y, new SKColor(gray, gray, gray, pixel.Alpha));
            }
        }
    }

    /// <summary>
    /// Simple contrast stretch — maps the darkest/lightest pixels to 0/255.
    /// </summary>
    private static void ApplyContrastStretch(SKBitmap bitmap)
    {
        byte min = 255, max = 0;

        // Find min/max luminance
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                byte lum = (byte)(0.299 * pixel.Red + 0.587 * pixel.Green + 0.114 * pixel.Blue);
                if (lum < min) min = lum;
                if (lum > max) max = lum;
            }
        }

        if (max == min) return; // Uniform image, nothing to stretch

        float range = max - min;

        // Stretch
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                byte r = (byte)(((pixel.Red - min) / range) * 255);
                byte g = (byte)(((pixel.Green - min) / range) * 255);
                byte b = (byte)(((pixel.Blue - min) / range) * 255);
                bitmap.SetPixel(x, y, new SKColor(r, g, b, pixel.Alpha));
            }
        }
    }
}
