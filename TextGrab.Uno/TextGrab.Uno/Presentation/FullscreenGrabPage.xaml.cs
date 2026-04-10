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
    private Point _startPoint;
    private IOcrService? _ocrService;
    private IScreenCaptureService? _captureService;
    private byte[]? _capturedScreenBytes;

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
        _ocrService = GetService<IOcrService>();
        _captureService = GetService<IScreenCaptureService>();

        // Maximize the window for fullscreen overlay effect
#if WINDOWS
        MaximizeWindow();
#endif

        // Load languages
        var langService = GetService<ILanguageService>();
        if (langService is not null)
        {
            foreach (var lang in langService.GetAllLanguages())
                LanguagesComboBox.Items.Add(lang.DisplayName);
            if (LanguagesComboBox.Items.Count > 0)
                LanguagesComboBox.SelectedIndex = 0;
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
                // Store raw bytes so they survive BitmapImage consuming the stream
                using var ms = new MemoryStream();
                await capturedStream.CopyToAsync(ms);
                _capturedScreenBytes = ms.ToArray();

                var bitmapImage = new BitmapImage();
                using var displayStream = new MemoryStream(_capturedScreenBytes);
                await bitmapImage.SetSourceAsync(displayStream.AsRandomAccessStream());
                BackgroundImage.Source = bitmapImage;
                StatusText.Text = "Draw a rectangle to capture text, or press Esc to cancel";

                // Apply shade overlay from settings
                var settings = GetService<IOptions<AppSettings>>();
                if (settings?.Value?.FsgShadeOverlay == false)
                    SelectionCanvas.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.Transparent);
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
        var appSettings = GetService<IOptions<AppSettings>>();
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

        // Draggable toolbar
        FloatingToolbar.ManipulationDelta += FloatingToolbar_ManipulationDelta;

        // Focus for keyboard input
        this.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        RestoreWindow();
#endif
        _capturedScreenBytes = null;
    }

    // --- Window management (Windows-only) ---

#if WINDOWS
    private void MaximizeWindow()
    {
        if (App.MainWindow is not null)
        {
            var appWindow = GetAppWindow();
            if (appWindow is not null)
            {
                appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            }
        }
    }

    private void MinimizeWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        ShowWindow(hwnd, 6); // SW_MINIMIZE
    }

    private void RestoreWindow()
    {
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
            SelectionCanvas.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Transparent);

            StatusText.Text = "Frozen — draw to capture, press F to unfreeze";

            // Re-capture the screen fresh (minimize → capture → restore)
            MinimizeWindow();
            await Task.Delay(200);

            using var capturedStream = await _captureService!.CaptureScreenAsync();

            MaximizeWindow();

            if (capturedStream is not null)
            {
                using var ms = new MemoryStream();
                await capturedStream.CopyToAsync(ms);
                _capturedScreenBytes = ms.ToArray();

                var bitmapImage = new BitmapImage();
                using var displayStream = new MemoryStream(_capturedScreenBytes);
                await bitmapImage.SetSourceAsync(displayStream.AsRandomAccessStream());
                BackgroundImage.Source = bitmapImage;
            }
        }
        else
        {
            // Restore shade overlay
            var settings = GetService<IOptions<AppSettings>>();
            if (settings?.Value?.FsgShadeOverlay != false)
                SelectionCanvas.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Windows.UI.Color.FromArgb(0x60, 0, 0, 0));

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
        }
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
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetCurrentPoint(SelectionCanvas).Position;

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double w = Math.Abs(currentPoint.X - _startPoint.X);
        double h = Math.Abs(currentPoint.Y - _startPoint.Y);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width = w;
        SelectionBorder.Height = h;
    }

    private async void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        SelectionCanvas.ReleasePointerCapture(e.Pointer);

        double w = SelectionBorder.Width;
        double h = SelectionBorder.Height;

        // Small click = single-word selection
        if (w < 5 || h < 5)
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            await RunWordSelectionAsync(_startPoint);
            return;
        }

        var region = new Rect(
            Canvas.GetLeft(SelectionBorder),
            Canvas.GetTop(SelectionBorder),
            w, h);

        await RunOcrOnRegionAsync(region);
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

        var subset = new SKBitmap();
        if (!skBitmap.ExtractSubset(subset, new SKRectI(x, y, x + w, y + h)))
            return null;

        var stream = new MemoryStream();
        using var image = SKImage.FromBitmap(subset);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Position = 0;

        subset.Dispose();
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

            var result = await _ocrService.RecognizeAsync(imageStream);
            if (result is null)
            {
                StatusText.Text = "OCR returned no results — try a larger selection";
                SelectionBorder.Visibility = Visibility.Collapsed;
                return;
            }

            string text = !string.IsNullOrWhiteSpace(result.CleanedOutput)
                ? result.CleanedOutput
                : result.RawOutput;

            // Try barcode detection if enabled
            var settings = GetService<IOptions<AppSettings>>();
            if (settings?.Value?.ReadBarcodesOnGrab == true && imageStream.CanSeek)
            {
                imageStream.Position = 0;
                using var barcodeMs = new MemoryStream();
                await imageStream.CopyToAsync(barcodeMs);
                var barcodeService = GetService<IBarcodeService>();
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

            // Apply mode
            if (SingleLineModeRadio.IsChecked == true || SingleLineCtxItem.IsChecked)
                text = text.MakeStringSingleLine();

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
            var result = await _ocrService.RecognizeAsync(stream);

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
    /// Common finish: copy to clipboard, notify, optionally navigate to EditText.
    /// </summary>
    private async Task FinishGrabAsync(string text, OcrEngineKind engine)
    {
        ClipboardHelper.CopyText(text);

        var notificationService = GetService<INotificationService>();
        notificationService?.ShowSuccess($"Copied: {(text.Length > 60 ? text[..60] + "..." : text)}");

        StatusText.Text = $"Copied {text.Length} chars ({engine})";

        await Task.Delay(500);

        if (SendToEtwToggle.IsChecked == true)
        {
            PendingTextForEditText = text;
        }

        Cancel_Click(this, new RoutedEventArgs());
    }

    // --- Fallback handlers ---

    private async void OcrFromFile_Click(object sender, RoutedEventArgs e)
    {
        var fileService = GetService<IFileService>();
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

    private T? GetService<T>() where T : class
        => ((App)Application.Current).Host?.Services.GetService<T>();
}
