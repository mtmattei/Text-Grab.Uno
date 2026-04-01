using Windows.ApplicationModel.DataTransfer;

namespace TextGrab.Shared;

/// <summary>
/// Consolidates clipboard operations used across multiple pages.
/// Replaces 13+ instances of DataPackage/SetText/SetContent three-liner.
/// </summary>
public static class ClipboardHelper
{
    public static void CopyText(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
    }

    public static async Task<MemoryStream?> GetBitmapStreamFromClipboardAsync()
    {
        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap))
            return null;

        var streamRef = await content.GetBitmapAsync();
        using var randomStream = await streamRef.OpenReadAsync();
        var memStream = new MemoryStream();
        await randomStream.AsStreamForRead().CopyToAsync(memStream);
        memStream.Position = 0;
        return memStream;
    }

    public static bool HasBitmap()
    {
        var content = Clipboard.GetContent();
        return content.Contains(StandardDataFormats.Bitmap);
    }
}
