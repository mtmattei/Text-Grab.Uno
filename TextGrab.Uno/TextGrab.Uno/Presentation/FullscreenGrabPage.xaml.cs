using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace TextGrab.Presentation;

public sealed partial class FullscreenGrabPage : Page
{
    private bool _isSelecting;
    private bool _isFrozen;
    private bool _grabFrameMode;
    private Point _startPoint;
    private IOcrService? _ocrService;
    private IScreenCaptureService? _captureService;
    private byte[]? _capturedScreenBytes;

    // Zoom state
    private const double MinZoom = 1.0;
    private const double MaxZoom = 16.0;
    private const double ZoomStep = 1.25;
    private double _zoom = 1.0;

    /// <summary>
    /// Static property to pass captured text to EditTextPage after navigation.
    /// </summary>
    internal static string? PendingTextForEditText { get; set; }

    public FullscreenGrabPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ocrService = this.GetService<IOcrService>();
        _captureService = this.GetService<IScreenCaptureService>();

        // Maximize the window for fullscreen overlay effect
#if WINDOWS
        MaximizeWindow();
#endif

        // Load languages — store ILanguage objects so selection drives OCR engine routing
        var langService = this.GetService<ILanguageService>();
        if (langService is not null)
        {
            var allLanguages = langService.GetAllLanguages();
            var currentLang = langService.GetOcrLanguage();
            foreach (var lang in allLanguages)
                LanguagesComboBox.Items.Add(lang);

            // Select last-used language
            var selected = allLanguages.FirstOrDefault(l => l.LanguageTag == currentLang.LanguageTag);
            LanguagesComboBox.SelectedItem = selected ?? allLanguages.FirstOrDefault();
        }

#if WINDOWS
        if (_captureService?.IsSupported == true)
        {
            StatusText.Text = "Capturing screen...";

            // Brief delay to let window maximize before capture
            await Task.Delay(100);

            // Minimize our window, capture screen, then restore
            MinimizeWindow();
            await Task.Delay(200);

            using var capturedStream = await _captureService.CaptureScreenAsync();

            MaximizeWindow();

            if (capturedStream is not null)
            {
                await StoreAndDisplayScreenshotAsync(capturedStream);
                StatusText.Text = "Draw a rectangle to capture text, or press Esc to cancel";

                // Apply shade overlay from settings
                var settings = this.GetService<IOptions<AppSettings>>();
                bool shadeOn = settings?.Value?.FsgShadeOverlay != false;
                SetShadesEnabled(shadeOn);
                LayoutShadesForFullCanvas();
            }
            else
            {
                StatusText.Text = "Screen capture failed";
            }
        }
        else
        {
            FallbackPanel.Visibility = Visibility.Visible;
            SelectionCanvas.Visibility = Visibility.Collapsed;
            FloatingToolbar.Visibility = Visibility.Collapsed;
        }
#else
        FallbackPanel.Visibility = Visibility.Visible;
        SelectionCanvas.Visibility = Visibility.Collapsed;
        FloatingToolbar.Visibility = Visibility.Collapsed;
#endif

        // Apply default mode from settings
        var appSettings = this.GetService<IOptions<AppSettings>>();
        if (appSettings?.Value is not null)
        {
            SendToEtwToggle.IsChecked = appSettings.Value.FsgSendEtwToggle;
            switch (appSettings.Value.FsgDefaultMode)
            {
                case "SingleLine": SingleLineModeRadio.IsChecked = true; break;
                case "Table": TableModeRadio.IsChecked = true; break;
                default: NormalModeRadio.IsChecked = true; break;
            }
        }

        // Populate Post-Grab Actions menu
        PopulatePostGrabActionsMenu(appSettings?.Value?.PostGrabActionsEnabled ?? "");

        // Draggable toolbar
        FloatingToolbar.ManipulationDelta += FloatingToolbar_ManipulationDelta;

        // Re-layout shades whenever the canvas is resized (initial layout + window size change)
        SelectionCanvas.SizeChanged += (_, _) =>
        {
            if (SelectionBorder.Visibility == Visibility.Visible && SelectionBorder.Width > 0 && SelectionBorder.Height > 0)
                LayoutShadesAroundSelection(
                    Canvas.GetLeft(SelectionBorder), Canvas.GetTop(SelectionBorder),
                    SelectionBorder.Width, SelectionBorder.Height);
            else
                LayoutShadesForFullCanvas();
        };

        // Focus for keyboard input
        this.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        RestoreWindow();
#endif
        FloatingToolbar.ManipulationDelta -= FloatingToolbar_ManipulationDelta;
        _capturedScreenBytes = null;

        // Reset zoom so next navigation starts fresh
        _zoom = 1.0;
        ZoomScale.ScaleX = 1.0;
        ZoomScale.ScaleY = 1.0;
        ZoomTranslate.X = 0;
        ZoomTranslate.Y = 0;
    }

    // --- Window management (Windows-only) ---

#if WINDOWS
    // Win32 window flags for HWND_TOPMOST
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private static readonly nint HWND_TOPMOST = -1;
    private static readonly nint HWND_NOTOPMOST = -2;

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClipCursor(ref RECT lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClipCursor(nint lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    private void ClipCursorToWindow()
    {
        if (App.MainWindow is null) return;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        if (GetWindowRect(hwnd, out RECT rect))
            ClipCursor(ref rect);
    }

    private void UnclipCursor()
    {
        ClipCursor(nint.Zero);
    }

    private void MaximizeWindow()
    {
        if (App.MainWindow is not null)
        {
            var appWindow = GetAppWindow();
            if (appWindow is not null)
            {
                appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            }

            // Enforce topmost so FSG overlay stays above all other windows
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
    }

    private void MinimizeWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        ShowWindow(hwnd, 6); // SW_MINIMIZE
    }

    private void RestoreWindow()
    {
        // Clear topmost flag
        if (App.MainWindow is not null)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        var appWindow = GetAppWindow();
        if (appWindow is not null)
        {
            appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
        }
    }

    private static Microsoft.UI.Windowing.AppWindow? GetAppWindow()
    {
        if (App.MainWindow is null) return null;
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);
#endif

    // --- Screenshot capture helper ---

    private async Task StoreAndDisplayScreenshotAsync(Stream capturedStream)
    {
        using var ms = new MemoryStream();
        await capturedStream.CopyToAsync(ms);
        _capturedScreenBytes = ms.ToArray();

        var bitmapImage = new BitmapImage();
        using var displayStream = new MemoryStream(_capturedScreenBytes);
        await bitmapImage.SetSourceAsync(displayStream.AsRandomAccessStream());
        BackgroundImage.Source = bitmapImage;
    }

    // --- Toolbar drag ---

    private void FloatingToolbar_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        double newX = ToolbarTranslate.X + e.Delta.Translation.X;
        double newY = ToolbarTranslate.Y + e.Delta.Translation.Y;

        // Clamp so toolbar stays within page bounds
        double pageWidth = this.ActualWidth;
        double pageHeight = this.ActualHeight;
        double toolbarWidth = FloatingToolbar.ActualWidth;
        double toolbarHeight = FloatingToolbar.ActualHeight;

        double halfToolbar = toolbarWidth / 2.0;
        double centerX = pageWidth / 2.0;

        double minX = -(centerX - 50);
        double maxX = centerX - 50;
        double minY = -12;
        double maxY = pageHeight - toolbarHeight - 12;

        ToolbarTranslate.X = Math.Clamp(newX, minX, maxX);
        ToolbarTranslate.Y = Math.Clamp(newY, minY, maxY);

        e.Handled = true;
    }

    // --- Shade rectangles (darken outside selection) ---

    private void SetShadesEnabled(bool enabled)
    {
        var vis = enabled ? Visibility.Visible : Visibility.Collapsed;
        ShadeTop.Visibility = vis;
        ShadeBottom.Visibility = vis;
        ShadeLeft.Visibility = vis;
        ShadeRight.Visibility = vis;
    }

    private void LayoutShadesForFullCanvas()
    {
        // No selection — ShadeTop covers the whole canvas, others are zero
        double w = SelectionCanvas.ActualWidth;
        double h = SelectionCanvas.ActualHeight;

        Canvas.SetLeft(ShadeTop, 0);
        Canvas.SetTop(ShadeTop, 0);
        ShadeTop.Width = w;
        ShadeTop.Height = h;

        ShadeBottom.Width = 0;
        ShadeBottom.Height = 0;
        ShadeLeft.Width = 0;
        ShadeLeft.Height = 0;
        ShadeRight.Width = 0;
        ShadeRight.Height = 0;
    }

    private void LayoutShadesAroundSelection(double selLeft, double selTop, double selWidth, double selHeight)
    {
        double canvasW = SelectionCanvas.ActualWidth;
        double canvasH = SelectionCanvas.ActualHeight;
        double selRight = selLeft + selWidth;
        double selBottom = selTop + selHeight;

        // Top strip: full width, y=0 to selTop
        Canvas.SetLeft(ShadeTop, 0);
        Canvas.SetTop(ShadeTop, 0);
        ShadeTop.Width = canvasW;
        ShadeTop.Height = Math.Max(0, selTop);

        // Bottom strip: full width, y=selBottom to canvasH
        Canvas.SetLeft(ShadeBottom, 0);
        Canvas.SetTop(ShadeBottom, selBottom);
        ShadeBottom.Width = canvasW;
        ShadeBottom.Height = Math.Max(0, canvasH - selBottom);

        // Left strip: x=0 to selLeft, within selection's y range
        Canvas.SetLeft(ShadeLeft, 0);
        Canvas.SetTop(ShadeLeft, selTop);
        ShadeLeft.Width = Math.Max(0, selLeft);
        ShadeLeft.Height = Math.Max(0, selHeight);

        // Right strip: x=selRight to canvasW, within selection's y range
        Canvas.SetLeft(ShadeRight, selRight);
        Canvas.SetTop(ShadeRight, selTop);
        ShadeRight.Width = Math.Max(0, canvasW - selRight);
        ShadeRight.Height = Math.Max(0, selHeight);
    }

    // --- Zoom (mouse wheel) ---

    private void ZoomWrapper_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(ZoomWrapper).Properties;
        int delta = props.MouseWheelDelta;
        if (delta == 0) return;

        var cursorPoint = e.GetCurrentPoint(ZoomWrapper).Position;

        double oldZoom = _zoom;
        double newZoom = delta > 0 ? _zoom * ZoomStep : _zoom / ZoomStep;
        newZoom = Math.Clamp(newZoom, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - oldZoom) < 0.0001)
        {
            e.Handled = true;
            return;
        }

        // Zoom to cursor: translate so the point under the cursor stays put
        // Derived from: screen = (content * zoom) + translate
        double scaleDelta = newZoom / oldZoom;
        ZoomTranslate.X = cursorPoint.X - scaleDelta * (cursorPoint.X - ZoomTranslate.X);
        ZoomTranslate.Y = cursorPoint.Y - scaleDelta * (cursorPoint.Y - ZoomTranslate.Y);

        _zoom = newZoom;
        ZoomScale.ScaleX = _zoom;
        ZoomScale.ScaleY = _zoom;

        // Clamp translate so we don't pan past content bounds
        ClampZoomTranslate();

        e.Handled = true;
    }

    private void ClampZoomTranslate()
    {
        if (_zoom <= 1.0)
        {
            ZoomTranslate.X = 0;
            ZoomTranslate.Y = 0;
            return;
        }

        double pageW = this.ActualWidth;
        double pageH = this.ActualHeight;
        double extraW = pageW * (_zoom - 1);
        double extraH = pageH * (_zoom - 1);

        ZoomTranslate.X = Math.Clamp(ZoomTranslate.X, -extraW, 0);
        ZoomTranslate.Y = Math.Clamp(ZoomTranslate.Y, -extraH, 0);
    }

    // --- Freeze ---

    private async void Freeze_Click(object sender, RoutedEventArgs e)
    {
        await ToggleFreezeAsync();
    }

    private async Task ToggleFreezeAsync()
    {
#if WINDOWS
        if (_captureService?.IsSupported != true) return;

        _isFrozen = !_isFrozen;
        FreezeToggle.IsChecked = _isFrozen;
        FreezeCtxItem.IsChecked = _isFrozen;

        if (_isFrozen)
        {
            // Remove overlay dimming for a clean "frozen screenshot" look
            SetShadesEnabled(false);
            FloatingToolbar.Visibility = Visibility.Collapsed;

            StatusText.Text = "Frozen — draw to capture, press F to unfreeze";

            // Re-capture the screen fresh (minimize → capture → restore)
            MinimizeWindow();
            await Task.Delay(200);

            using var capturedStream = await _captureService!.CaptureScreenAsync();

            MaximizeWindow();

            if (capturedStream is not null)
            {
                await StoreAndDisplayScreenshotAsync(capturedStream);
            }
        }
        else
        {
            // Restore shade overlay and toolbar
            var settings = this.GetService<IOptions<AppSettings>>();
            bool shadeOn = settings?.Value?.FsgShadeOverlay != false;
            SetShadesEnabled(shadeOn);
            LayoutShadesForFullCanvas();
            FloatingToolbar.Visibility = Visibility.Visible;

            StatusText.Text = "Draw a rectangle to capture text, or press Esc to cancel";
        }

        this.Focus(FocusState.Programmatic);
#endif
    }

    // --- Keyboard ---

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape:
                Cancel_Click(sender, e);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.S:
                SingleLineModeRadio.IsChecked = true;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.N:
                NormalModeRadio.IsChecked = true;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.T:
                TableModeRadio.IsChecked = true;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.E:
                SendToEtwToggle.IsChecked = !SendToEtwToggle.IsChecked;
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F:
                _ = ToggleFreezeAsync();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.G:
                ToggleGrabFrameMode();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Number1:
            case Windows.System.VirtualKey.Number2:
            case Windows.System.VirtualKey.Number3:
            case Windows.System.VirtualKey.Number4:
            case Windows.System.VirtualKey.Number5:
            case Windows.System.VirtualKey.Number6:
            case Windows.System.VirtualKey.Number7:
            case Windows.System.VirtualKey.Number8:
            case Windows.System.VirtualKey.Number9:
                int idx = (int)e.Key - (int)Windows.System.VirtualKey.Number1;
                if (idx < LanguagesComboBox.Items.Count)
                {
                    LanguagesComboBox.SelectedIndex = idx;
                    if (LanguagesComboBox.SelectedItem is ILanguage lang)
                        StatusText.Text = $"Language: {lang.DisplayName}";
                }
                e.Handled = true;
                break;
        }
    }

    private void ToggleGrabFrameMode()
    {
        _grabFrameMode = !_grabFrameMode;
        GrabFrameToggle.IsChecked = _grabFrameMode;
        StatusText.Text = _grabFrameMode
            ? "Grab Frame mode — draw a region to open it in Grab Frame"
            : "Draw a rectangle to capture text, or press Esc to cancel";
    }

    // --- Canvas selection ---

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isSelecting = true;
        _startPoint = e.GetCurrentPoint(SelectionCanvas).Position;

        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, _startPoint.X);
        Canvas.SetTop(SelectionBorder, _startPoint.Y);
        SelectionBorder.Width = 0;
        SelectionBorder.Height = 0;

        SelectionCanvas.CapturePointer(e.Pointer);
#if WINDOWS
        ClipCursorToWindow();
#endif
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetCurrentPoint(SelectionCanvas).Position;
        bool shiftDown = (e.KeyModifiers & Windows.System.VirtualKeyModifiers.Shift)
            == Windows.System.VirtualKeyModifiers.Shift;

        if (shiftDown && SelectionBorder.Width > 0 && SelectionBorder.Height > 0)
        {
            // Shift+drag: pan the existing selection rectangle
            double dx = currentPoint.X - _startPoint.X;
            double dy = currentPoint.Y - _startPoint.Y;

            double newLeft = Math.Max(0, Canvas.GetLeft(SelectionBorder) + dx);
            double newTop = Math.Max(0, Canvas.GetTop(SelectionBorder) + dy);

            // Clamp to canvas bounds
            newLeft = Math.Min(newLeft, SelectionCanvas.ActualWidth - SelectionBorder.Width);
            newTop = Math.Min(newTop, SelectionCanvas.ActualHeight - SelectionBorder.Height);

            Canvas.SetLeft(SelectionBorder, newLeft);
            Canvas.SetTop(SelectionBorder, newTop);
            _startPoint = currentPoint;
            LayoutShadesAroundSelection(newLeft, newTop, SelectionBorder.Width, SelectionBorder.Height);
            return;
        }

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double w = Math.Abs(currentPoint.X - _startPoint.X);
        double h = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;

        LayoutShadesAroundSelection(x, y, w, h);
        UpdateSizeReadout(x, y, w, h);
    }

    private void UpdateSizeReadout(double selLeft, double selTop, double selWidth, double selHeight)
    {
        if (selWidth < 5 || selHeight < 5)
        {
            SizeReadout.Visibility = Visibility.Collapsed;
            return;
        }

        // Physical pixels for accurate measurement
        double scale = XamlRoot?.RasterizationScale ?? 1.0;
        int pxW = (int)(selWidth * scale);
        int pxH = (int)(selHeight * scale);

        SizeReadoutText.Text = $"{pxW} × {pxH}";
        SizeReadout.Visibility = Visibility.Visible;

        // Position just below and to the right of the selection's bottom-right corner,
        // but keep within canvas bounds
        double canvasW = SelectionCanvas.ActualWidth;
        double canvasH = SelectionCanvas.ActualHeight;
        double labelX = selLeft + selWidth + 6;
        double labelY = selTop + selHeight + 6;

        // If off-screen right, tuck inside the selection's bottom-right
        if (labelX + 80 > canvasW)
            labelX = selLeft + selWidth - 80;
        if (labelY + 20 > canvasH)
            labelY = selTop + selHeight - 22;

        Canvas.SetLeft(SizeReadout, Math.Max(0, labelX));
        Canvas.SetTop(SizeReadout, Math.Max(0, labelY));
    }

    private async void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        SelectionCanvas.ReleasePointerCapture(e.Pointer);
#if WINDOWS
        UnclipCursor();
#endif

        double w = SelectionBorder.Width;
        double h = SelectionBorder.Height;

        // Small click = single-word selection
        if (w < 5 || h < 5)
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            SizeReadout.Visibility = Visibility.Collapsed;
            LayoutShadesForFullCanvas();
            await RunWordSelectionAsync(_startPoint);
            return;
        }

        SizeReadout.Visibility = Visibility.Collapsed;

        var region = new Rect(
            Canvas.GetLeft(SelectionBorder),
            Canvas.GetTop(SelectionBorder),
            w, h);

        if (_grabFrameMode)
        {
            OpenRegionInGrabFrame(region);
            return;
        }

        await RunOcrOnRegionAsync(region);
    }

    private void OpenRegionInGrabFrame(Rect region)
    {
        using var cropped = CropRegionFromScreenshot(region);
        if (cropped is null)
        {
            StatusText.Text = "Failed to crop region";
            return;
        }

        using var ms = new MemoryStream();
        cropped.CopyTo(ms);
        GrabFramePage.PendingImageBytes = ms.ToArray();

        Cancel_Click(this, new RoutedEventArgs());
        this.Frame?.Navigate(typeof(GrabFramePage));
    }

    private void GrabFrame_Click(object sender, RoutedEventArgs e)
    {
        ToggleGrabFrameMode();
    }

    // --- OCR ---

    /// <summary>
    /// Crops the selected region from the stored screenshot using SkiaSharp,
    /// applying DPI scaling for accurate pixel coordinates.
    /// </summary>
    private Stream? CropRegionFromScreenshot(Rect dipRegion)
    {
        if (_capturedScreenBytes is null) return null;

        using var skBitmap = SKBitmap.Decode(_capturedScreenBytes);
        if (skBitmap is null) return null;

        // Scale DIP coordinates to physical pixels
        double scale = this.XamlRoot?.RasterizationScale ?? 1.0;
        int x = (int)(dipRegion.X * scale);
        int y = (int)(dipRegion.Y * scale);
        int w = (int)(dipRegion.Width * scale);
        int h = (int)(dipRegion.Height * scale);

        // Clamp to bitmap bounds
        x = Math.Clamp(x, 0, skBitmap.Width - 1);
        y = Math.Clamp(y, 0, skBitmap.Height - 1);
        w = Math.Clamp(w, 1, skBitmap.Width - x);
        h = Math.Clamp(h, 1, skBitmap.Height - y);

        using var subset = new SKBitmap();
        if (!skBitmap.ExtractSubset(subset, new SKRectI(x, y, x + w, y + h)))
            return null;

        var stream = new MemoryStream();
        using var image = SKImage.FromBitmap(subset);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    private async Task RunOcrOnRegionAsync(Rect region)
    {
        if (_ocrService is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR...";

        try
        {
            // Crop from the stored screenshot (capture-once-crop-many pattern)
            using var imageStream = CropRegionFromScreenshot(region);

            if (imageStream is null)
            {
                StatusText.Text = "No image to OCR";
                return;
            }

            var selectedLang = LanguagesComboBox.SelectedItem as ILanguage;
            var result = await _ocrService.RecognizeAsync(imageStream, selectedLang);
            if (result is null)
            {
                StatusText.Text = "OCR returned no results — try a larger selection";
                SelectionBorder.Visibility = Visibility.Collapsed;
                return;
            }

            string text = result.GetBestText();

            // Try barcode detection if enabled
            var settings = this.GetService<IOptions<AppSettings>>();
            if (settings?.Value?.ReadBarcodesOnGrab == true && imageStream.CanSeek)
            {
                imageStream.Position = 0;
                using var barcodeMs = new MemoryStream();
                await imageStream.CopyToAsync(barcodeMs);
                var barcodeService = this.GetService<IBarcodeService>();
                if (barcodeService is not null)
                {
                    var barcodeText = await barcodeService.ReadBarcodeFromImageAsync(barcodeMs.ToArray());
                    if (!string.IsNullOrEmpty(barcodeText))
                    {
                        text = string.IsNullOrWhiteSpace(text)
                            ? $"[Barcode] {barcodeText}"
                            : $"{text}{Environment.NewLine}[Barcode] {barcodeText}";
                    }
                }
            }

            // Apply mode — Table takes precedence, then Single Line
            bool tableMode = TableModeRadio.IsChecked == true || TableCtxItem.IsChecked;
            bool singleLineMode = SingleLineModeRadio.IsChecked == true || SingleLineCtxItem.IsChecked;

            if (tableMode && result.StructuredResult is not null)
            {
                var lang = LanguagesComboBox.SelectedItem as ILanguage
                    ?? this.GetService<ILanguageService>()?.GetOcrLanguage();
                if (lang is not null)
                    text = OcrUtilities.FormatAsTable(result.StructuredResult, lang);
            }
            else if (singleLineMode)
            {
                text = text.MakeStringSingleLine();
            }

            await FinishGrabAsync(text, result.Engine);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"OCR failed: {ex.Message}";
        }
        finally
        {
            BusyRing.IsActive = false;
            SelectionBorder.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Single-click word selection: OCR the full image, find the word at the click point.
    /// </summary>
    private async Task RunWordSelectionAsync(Point clickPoint)
    {
        if (_ocrService is null || _capturedScreenBytes is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Detecting word...";

        try
        {
            using var stream = new MemoryStream(_capturedScreenBytes);
            var selectedLang = LanguagesComboBox.SelectedItem as ILanguage;
            var result = await _ocrService.RecognizeAsync(stream, selectedLang);

            if (result?.StructuredResult?.Lines is null)
            {
                StatusText.Text = "No text detected at click point";
                return;
            }

            // Scale click point to match OCR coordinate space (OCR works on physical pixels)
            double scale = this.XamlRoot?.RasterizationScale ?? 1.0;
            double px = clickPoint.X * scale;
            double py = clickPoint.Y * scale;

            // Find word whose bounding box contains the click
            string? foundWord = null;
            foreach (var line in result.StructuredResult.Lines)
            {
                foreach (var word in line.Words)
                {
                    if (word.BoundingBox.Contains(new Point(px, py)))
                    {
                        foundWord = word.Text;
                        break;
                    }
                }
                if (foundWord is not null) break;
            }

            if (string.IsNullOrWhiteSpace(foundWord))
            {
                StatusText.Text = "No word found at click point";
                return;
            }

            await FinishGrabAsync(foundWord, result.Engine);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Word detection failed: {ex.Message}";
        }
        finally
        {
            BusyRing.IsActive = false;
        }
    }

    /// <summary>
    /// Common finish: apply post-grab actions, copy to clipboard, notify, optionally navigate to EditText.
    /// </summary>
    private async Task FinishGrabAsync(string text, OcrEngineKind engine)
    {
        // Apply post-grab actions (Fix GUIDs, Trim lines, Remove dupes, Web Search)
        var settings = this.GetService<IOptions<AppSettings>>();
        if (settings?.Value is not null)
        {
            var enabled = PostGrabActionManager.ParseEnabled(settings.Value.PostGrabActionsEnabled);
            if (enabled.Count > 0)
                text = await PostGrabActionManager.ApplyEnabledActionsAsync(
                    text, enabled, settings.Value.WebSearchUrl);
        }

        ClipboardHelper.CopyText(text);

        // Save to history if enabled
        if (settings?.Value?.UseHistory == true && !string.IsNullOrWhiteSpace(text))
        {
            var historyService = this.GetService<IHistoryService>();
            if (historyService is not null)
            {
                var lang = LanguagesComboBox.SelectedItem as ILanguage;
                var info = new HistoryInfo(
                    Id: Guid.NewGuid().ToString("N"),
                    TextContent: text,
                    CaptureDateTime: DateTimeOffset.UtcNow,
                    SourceMode: "FullscreenGrab",
                    LanguageTag: lang?.LanguageTag ?? "");
                _ = historyService.SaveTextHistoryAsync(info);
            }
        }

        var notificationService = this.GetService<INotificationService>();
        notificationService?.ShowSuccess($"Copied: {(text.Length > 60 ? text[..60] + "..." : text)}");

        StatusText.Text = $"Copied {text.Length} chars ({engine})";

        await Task.Delay(500);

        if (SendToEtwToggle.IsChecked == true)
        {
            PendingTextForEditText = text;
        }

        Cancel_Click(this, new RoutedEventArgs());
    }

    // --- Post-grab actions menu ---

    private void PopulatePostGrabActionsMenu(string enabledCsv)
    {
        PostGrabActionsFlyout.Items.Clear();
        var enabled = PostGrabActionManager.ParseEnabled(enabledCsv);

        foreach (var action in PostGrabActionManager.AllActions)
        {
            var item = new ToggleMenuFlyoutItem
            {
                Text = action.Label,
                Icon = new FontIcon
                {
                    Glyph = action.Glyph,
                    FontFamily = (Microsoft.UI.Xaml.Media.FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
                },
                IsChecked = enabled.Contains(action.ActionId),
                Tag = action.ActionId,
            };
            item.Click += PostGrabActionItem_Click;
            PostGrabActionsFlyout.Items.Add(item);
        }
    }

    private async void PostGrabActionItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleMenuFlyoutItem item || item.Tag is not string key)
            return;

        var writable = this.GetService<IWritableOptions<AppSettings>>();
        if (writable is null) return;

        // Build the updated enabled set from current menu state
        var enabled = new HashSet<string>();
        foreach (var child in PostGrabActionsFlyout.Items.OfType<ToggleMenuFlyoutItem>())
            if (child.IsChecked && child.Tag is string k)
                enabled.Add(k);

        await writable.UpdateAsync(s => s with
        {
            PostGrabActionsEnabled = PostGrabActionManager.SerializeEnabled(enabled)
        });
    }

    // --- Fallback handlers ---

    private async void OcrFromFile_Click(object sender, RoutedEventArgs e)
    {
        var fileService = this.GetService<IFileService>();
        if (fileService is null || _ocrService is null) return;

        var imageData = await fileService.PickImageFileAsync();
        if (imageData is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR...";

        using var stream = new MemoryStream(imageData);
        var result = await _ocrService.RecognizeAsync(stream);

        if (result is not null)
        {
            var text = result.GetBestText();
            ClipboardHelper.CopyText(text);
            StatusText.Text = $"Copied {text.Length} chars";
        }
        else
        {
            StatusText.Text = "OCR returned no results";
        }

        BusyRing.IsActive = false;
    }

    private async void OcrFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (_ocrService is null) return;

        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap))
        {
            StatusText.Text = "No image in clipboard";
            return;
        }

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR on clipboard...";

        var streamRef = await content.GetBitmapAsync();
        using var randomStream = await streamRef.OpenReadAsync();
        using var memStream = new MemoryStream();
        await randomStream.AsStreamForRead().CopyToAsync(memStream);
        memStream.Position = 0;

        var result = await _ocrService.RecognizeAsync(memStream);

        if (result is not null)
        {
            var text = result.GetBestText();
            ClipboardHelper.CopyText(text);
            StatusText.Text = $"Copied {text.Length} chars";
        }
        else
        {
            StatusText.Text = "OCR returned no results";
        }

        BusyRing.IsActive = false;
    }

    // --- Navigation ---

    private void EscAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Cancel_Click(this, new RoutedEventArgs());
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Use Frame.Navigate directly — INavigator doesn't drive ShellPage's manual navigation
        this.Frame?.Navigate(typeof(EditTextPage));
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        this.Frame?.Navigate(typeof(SettingsPage));
    }

    // --- Language picker ---

    private async void LanguagesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguagesComboBox.SelectedItem is not ILanguage lang) return;

        // Persist last-used language so next launch and OcrService default match
        var writable = this.GetService<IWritableOptions<AppSettings>>();
        if (writable is not null)
            await writable.UpdateAsync(s => s with { LastUsedLang = lang.LanguageTag });
    }
}
