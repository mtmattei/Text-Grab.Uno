using Microsoft.UI.Xaml.Media.Imaging;
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
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _ocrService = GetService<IOcrService>();
        _captureService = GetService<IScreenCaptureService>();

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
            _capturedScreen = await _captureService.CaptureScreenAsync();
            if (_capturedScreen is not null)
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(_capturedScreen.AsRandomAccessStream());
                BackgroundImage.Source = bitmapImage;
                _capturedScreen.Position = 0; // Reset for OCR later
                StatusText.Text = "Draw a rectangle to capture text, or click a word";
            }
            else
            {
                StatusText.Text = "Screen capture failed — try OCR from file instead";
            }
        }
        else
        {
            FallbackPanel.Visibility = Visibility.Visible;
            SelectionCanvas.Visibility = Visibility.Collapsed;
        }
#else
        FallbackPanel.Visibility = Visibility.Visible;
        SelectionCanvas.Visibility = Visibility.Collapsed;
#endif

        // Apply settings
        var settings = GetService<IOptions<AppSettings>>();
        if (settings?.Value is not null)
        {
            SendToEtwToggle.IsChecked = settings.Value.FsgSendEtwToggle;
            switch (settings.Value.FsgDefaultMode)
            {
                case "SingleLine": SingleLineModeRadio.IsChecked = true; break;
                case "Table": TableModeRadio.IsChecked = true; break;
                default: NormalModeRadio.IsChecked = true; break;
            }
        }
    }

    private void Canvas_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
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

    private void Canvas_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
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

    private async void Canvas_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
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

        // Get selection region relative to the image
        var region = new Rect(
            Canvas.GetLeft(SelectionBorder),
            Canvas.GetTop(SelectionBorder),
            w, h);

        await RunOcrOnRegionAsync(region);
    }

    private async Task RunOcrOnRegionAsync(Rect region)
    {
        if (_ocrService is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR...";

        try
        {
            Stream? imageStream = null;

#if WINDOWS
            // Capture just the selected region
            if (_captureService is not null)
            {
                imageStream = await _captureService.CaptureRegionAsync(region);
            }
#endif
            // Fallback: use full captured screen (less accurate but works)
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
                StatusText.Text = "OCR returned no results";
                return;
            }

            string text = !string.IsNullOrWhiteSpace(result.CleanedOutput)
                ? result.CleanedOutput
                : result.RawOutput;

            // Apply mode
            if (SingleLineModeRadio.IsChecked == true)
                text = text.Replace(Environment.NewLine, " ").Replace("\n", " ");

            // Copy to clipboard
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);

            StatusText.Text = $"Copied {text.Length} chars ({result.Engine})";

            // Show notification
            var notificationService = GetService<INotificationService>();
            notificationService?.ShowSuccess($"Text copied: {(text.Length > 50 ? text[..50] + "..." : text)}");

            // Navigate to Edit Text if toggle is on
            if (SendToEtwToggle.IsChecked == true)
            {
                // TODO: Pass text data to EditText via navigation data
            }
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

    private async void OcrFromFile_Click(object sender, RoutedEventArgs e)
    {
        var fileService = GetService<IFileService>();
        if (fileService is null || _ocrService is null) return;

        var imageData = await fileService.PickImageFileAsync();
        if (imageData is null) return;

        BusyRing.IsActive = true;
        StatusText.Text = "Running OCR on image...";

        using var stream = new MemoryStream(imageData);
        var result = await _ocrService.RecognizeAsync(stream);

        if (result is not null)
        {
            string text = result.CleanedOutput ?? result.RawOutput;
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);
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
        StatusText.Text = "Running OCR on clipboard image...";

        var streamRef = await content.GetBitmapAsync();
        using var randomStream = await streamRef.OpenReadAsync();
        using var memStream = new MemoryStream();
        await randomStream.AsStreamForRead().CopyToAsync(memStream);
        memStream.Position = 0;

        var result = await _ocrService.RecognizeAsync(memStream);

        if (result is not null)
        {
            string text = result.CleanedOutput ?? result.RawOutput;
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);
            StatusText.Text = $"Copied {text.Length} chars";
        }
        else
        {
            StatusText.Text = "OCR returned no results";
        }

        BusyRing.IsActive = false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var navigator = GetService<INavigator>();
        if (navigator is not null)
            _ = navigator.NavigateRouteAsync(this, "EditText");
    }

    private T? GetService<T>() where T : class
        => ((App)Application.Current).Host?.Services.GetService<T>();
}
