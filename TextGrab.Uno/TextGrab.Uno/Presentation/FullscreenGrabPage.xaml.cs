using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace TextGrab.Presentation;

public sealed partial class FullscreenGrabPage : Page
{
    private bool _isSelecting;
    private Point _startPoint;
    private IOcrService? _ocrService;
    private IScreenCaptureService? _captureService;
    private Stream? _capturedScreen;

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

            _capturedScreen = await _captureService.CaptureScreenAsync();

            MaximizeWindow();

            if (_capturedScreen is not null)
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(_capturedScreen.AsRandomAccessStream());
                BackgroundImage.Source = bitmapImage;
                _capturedScreen.Position = 0;
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

        // Focus for keyboard input
        this.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        RestoreWindow();
#endif
        _capturedScreen?.Dispose();
        _capturedScreen = null;
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

        if (w < 5 || h < 5)
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            return;
        }

        var region = new Rect(
            Canvas.GetLeft(SelectionBorder),
            Canvas.GetTop(SelectionBorder),
            w, h);

        await RunOcrOnRegionAsync(region);
    }

    // --- OCR ---

    private async Task RunOcrOnRegionAsync(Rect region)
    {
        if (_ocrService is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR...";

        try
        {
            Stream? imageStream = null;

#if WINDOWS
            if (_captureService is not null)
                imageStream = await _captureService.CaptureRegionAsync(region);
#endif

            if (imageStream is null && _capturedScreen is not null)
            {
                _capturedScreen.Position = 0;
                imageStream = _capturedScreen;
            }

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

            // Try barcode detection if enabled and OCR returned little/no text
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
                text = text.Replace(Environment.NewLine, " ").Replace("\n", " ");

            // Copy to clipboard
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);

            // Notify
            var notificationService = GetService<INotificationService>();
            notificationService?.ShowSuccess($"Copied: {(text.Length > 60 ? text[..60] + "..." : text)}");

            StatusText.Text = $"Copied {text.Length} chars ({result.Engine})";

            // Auto-navigate back after successful grab
            await Task.Delay(500);

            if (SendToEtwToggle.IsChecked == true)
            {
                // Navigate to EditText — TODO: pass text data
            }

            Cancel_Click(this, new RoutedEventArgs());
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
            var dp = new DataPackage();
            dp.SetText(result.CleanedOutput ?? result.RawOutput);
            Clipboard.SetContent(dp);
            StatusText.Text = $"Copied {(result.CleanedOutput ?? result.RawOutput).Length} chars";
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
            var dp = new DataPackage();
            dp.SetText(result.CleanedOutput ?? result.RawOutput);
            Clipboard.SetContent(dp);
            StatusText.Text = $"Copied {(result.CleanedOutput ?? result.RawOutput).Length} chars";
        }
        else
        {
            StatusText.Text = "OCR returned no results";
        }

        BusyRing.IsActive = false;
    }

    // --- Navigation ---

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var navigator = GetService<INavigator>();
        _ = navigator?.NavigateRouteAsync(this, "EditText");
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var navigator = GetService<INavigator>();
        _ = navigator?.NavigateRouteAsync(this, "Settings");
    }

    private T? GetService<T>() where T : class
        => ((App)Application.Current).Host?.Services.GetService<T>();
}
