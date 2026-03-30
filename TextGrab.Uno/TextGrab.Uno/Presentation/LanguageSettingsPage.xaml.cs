namespace TextGrab.Presentation;

public sealed partial class LanguageSettingsPage : Page
{
    public LanguageSettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        await LoadWindowsLanguagesAsync();
        await CheckWindowsAiStatusAsync();
        await CheckTesseractLanguagesAsync();
#else
        PlatformInfoText.Visibility = Visibility.Visible;
        StatusTextBlock.Text = "N/A";
        ReasonTextBlock.Text = "Windows AI OCR is only available on Windows Desktop.";
#endif
    }

#if WINDOWS
    private async Task LoadWindowsLanguagesAsync()
    {
        try
        {
            var langs = Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages;
            foreach (var lang in langs)
            {
                WindowsLanguagesListView.Items.Add(lang.DisplayName);
            }
        }
        catch
        {
            WindowsLanguagesListView.Items.Add("Could not load Windows OCR languages.");
        }

        await Task.CompletedTask;
    }

    private async Task CheckWindowsAiStatusAsync()
    {
        // Check if Windows AI OCR is available on this device
        try
        {
            var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            if (arch == System.Runtime.InteropServices.Architecture.Arm64)
            {
                StatusTextBlock.Text = "Available";
                ReasonTextBlock.Text = "ARM64 device detected — Windows AI OCR may be available.";
            }
            else
            {
                StatusTextBlock.Text = "Not Available";
                ReasonTextBlock.Text = "Windows AI OCR requires an ARM64 Copilot+ PC.";
            }
        }
        catch
        {
            StatusTextBlock.Text = "Unknown";
            ReasonTextBlock.Text = "Could not determine Windows AI status.";
        }

        await Task.CompletedTask;
    }

    private async Task CheckTesseractLanguagesAsync()
    {
        var model = GetModel();
        if (model is null) return;

        var useTess = await model.UseTesseract;
        if (!useTess) return;

        TesseractLanguagesPanel.Visibility = Visibility.Visible;

        var tessPath = await model.TesseractPath ?? "";
        if (string.IsNullOrEmpty(tessPath)) return;

        // Try to list Tesseract languages from tessdata folder
        var tessDir = System.IO.Path.GetDirectoryName(tessPath);
        if (tessDir is null) return;

        var tessDataDir = System.IO.Path.Combine(tessDir, "tessdata");
        if (System.IO.Directory.Exists(tessDataDir))
        {
            var trainedDataFiles = System.IO.Directory.GetFiles(tessDataDir, "*.traineddata");
            foreach (var file in trainedDataFiles)
            {
                TesseractLanguagesListView.Items.Add(
                    System.IO.Path.GetFileNameWithoutExtension(file));
            }
        }
    }
#endif

    private LanguageSettingsModel? GetModel() =>
        (DataContext as LanguageSettingsViewModel)?.Model;
}
