#if WINDOWS
using System.Runtime.InteropServices;
using SkiaSharp;

namespace TextGrab.Services;

/// <summary>
/// Windows screen capture using P/Invoke + SkiaSharp (no System.Drawing dependency).
/// </summary>
public class WindowsScreenCaptureService : IScreenCaptureService
{
    public bool IsSupported => true;

    public Task<Stream?> CaptureScreenAsync(CancellationToken ct = default)
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        return CaptureRegionAsync(new Windows.Foundation.Rect(0, 0, width, height), ct);
    }

    public Task<Stream?> CaptureRegionAsync(Windows.Foundation.Rect region, CancellationToken ct = default)
    {
        return Task.Run<Stream?>(() =>
        {
            try
            {
                int x = (int)region.X;
                int y = (int)region.Y;
                int w = (int)region.Width;
                int h = (int)region.Height;

                if (w <= 0 || h <= 0) return null;

                IntPtr hdcScreen = GetDC(IntPtr.Zero);
                IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, w, h);
                IntPtr hOld = SelectObject(hdcMem, hBitmap);

                BitBlt(hdcMem, 0, 0, w, h, hdcScreen, x, y, SRCCOPY);

                SelectObject(hdcMem, hOld);

                // Convert HBITMAP to SkiaSharp bitmap
                var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
                var skBitmap = new SKBitmap(info);

                var bmi = new BITMAPINFO
                {
                    biSize = 40,
                    biWidth = w,
                    biHeight = -h, // Top-down
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0, // BI_RGB
                };

                GetDIBits(hdcMem, hBitmap, 0, (uint)h, skBitmap.GetPixels(), ref bmi, 0);

                DeleteObject(hBitmap);
                DeleteDC(hdcMem);
                ReleaseDC(IntPtr.Zero, hdcScreen);

                // Encode to PNG
                var stream = new MemoryStream();
                using var image = SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                data.SaveTo(stream);
                stream.Position = 0;

                skBitmap.Dispose();
                return (Stream)stream;
            }
            catch
            {
                return null;
            }
        }, ct);
    }

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr ho);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines, IntPtr lpvBits, ref BITMAPINFO lpbmi, uint usage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }
}
#endif
